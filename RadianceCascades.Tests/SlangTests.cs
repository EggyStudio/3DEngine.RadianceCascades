using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The library's Slang compiled by the tests' kernels that import it to SPIR-V, WGSL, HLSL and the
/// Metal Shading Language, which Vulkan, WebGPU, Direct3D and Metal read, with no GPU.
/// </summary>
/// <remarks>
/// <para>
/// DXIL and a Metal library are compiled from the HLSL and the Metal by each platform's own
/// compiler, dxc and Apple's metal, which slangc calls where they are installed and the tests do
/// not take, so the tests compile the sources slangc writes itself.
/// </para>
/// <para>
/// A file of the library that no kernel calls would be checked by the compiler's parser alone, and
/// a function that one target cannot generate shows only when a kernel calls it, so every file is
/// imported by a kernel of the tests that calls each of its functions.
/// </para>
/// </remarks>
public sealed class SlangTests
{
    /// <summary>The tests' kernels by file name.</summary>
    public static TheoryData<string> Kernels()
    {
        var kernels = new TheoryData<string>();
        foreach (var kernel in Directory.EnumerateFiles(Repository.TestKernels, "*.slang").Order())
            kernels.Add(Path.GetFileName(kernel));
        return kernels;
    }

    [SlangTheory]
    [MemberData(nameof(Kernels))]
    public void A_Kernel_Of_The_Library_Compiles_To_SpirV_Wgsl_Hlsl_And_Metal_With_No_Warning(string kernel)
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-slang-");
        try
        {
            var source = Path.Combine(Repository.TestKernels, kernel);
            foreach (var (target, extension) in new[] { ("spirv", "spv"), ("wgsl", "wgsl"), ("hlsl", "hlsl"), ("metal", "metal") })
            {
                var output = Path.Combine(folder.FullName, $"{Path.GetFileNameWithoutExtension(kernel)}.{extension}");
                var (code, said) = Slangc.Compile(source, target, output);
                Assert.True(code == 0, $"{kernel} to {target} exited {code}:\n{said}");
                Assert.True(new FileInfo(output).Length > 0, $"{kernel} to {target} wrote nothing");
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [SlangTheory]
    [MemberData(nameof(Kernels))]
    public void A_Kernel_Of_The_Library_Keeps_No_Array_In_A_Threads_Own_Memory(string kernel)
    {
        var held = ThreadArraysOf(File.ReadAllText(Path.Combine(Repository.TestKernels, kernel)));
        Assert.True(held.Count == 0, $"{kernel} keeps an array in a thread's own memory:\n" + string.Join("\n", held));
    }

    // A field that holds every cascade's corner and cell by value, as the binding did whose reads
    // one macOS device drew wrong, so the scan above is seen to find what it looks for.
    [SlangTheory]
    [InlineData(2)]
    [InlineData(8)]
    public void A_Table_Of_Cascades_A_Field_Holds_By_Value_Is_Found_In_A_Threads_Own_Memory(int cascades)
    {
        var kernel = $$"""
            import rc_field_read;
            RWStructuredBuffer<float> cells;
            RWStructuredBuffer<float4> table;
            RWStructuredBuffer<float> read;
            struct TableField : IFieldVolume
            {
                float4 corners[{{cascades}}];
                int cascadeOf(float3 p, float margin)
                {
                    for (int c = 0; c < {{cascades}}; c++)
                    {
                        float3 q = (p - corners[c].xyz) / corners[c].w;
                        if (all(q >= margin) && all(q <= 32.0 - margin))
                            return c;
                    }
                    return -1;
                }
                float4 cornerAndCell(int cascade) { return corners[cascade]; }
                int size() { return 32; }
                float4 cellAt(int cascade, int3 at)
                {
                    int i = (((cascade * 32 + at.z) * 32 + at.y) * 32 + at.x) * 4;
                    return float4(cells[i], cells[i + 1], cells[i + 2], cells[i + 3]);
                }
                float4 between(int cascade, float3 at) { return cellAt(cascade, int3(at)); }
                float3 secondAt(int cascade, int3 at) { return float3(0.0); }
            };
            [shader("compute")]
            [numthreads(1, 1, 1)]
            void test(uint3 id : SV_DispatchThreadID)
            {
                TableField field;
                for (int c = 0; c < {{cascades}}; c++)
                    field.corners[c] = table[c];
                float3 p = float3(read[0], read[1], read[2]);
                NoSteps none;
                read[3] = fieldDistanceIn(field, field.cascadeOf(p, 1.0), p);
                read[4] = traceField(field, p, float3(1.0, 0.0, 0.0), 0.0, 10.0, none);
            }
            """;
        Assert.NotEmpty(ThreadArraysOf(kernel));
    }

    // The variables a kernel's SPIR-V keeps in a thread's own memory, its Function and Private
    // storage, that hold an array or a struct with one, read from the assembly slangc writes.
    // SPIR-V is the one target whose assembly names each variable's storage, and slangc lowers
    // every target from the same code.
    private static List<string> ThreadArraysOf(string kernel)
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-slang-");
        try
        {
            var source = Path.Combine(folder.FullName, "kernel.slang");
            var output = Path.Combine(folder.FullName, "kernel.spvasm");
            File.WriteAllText(source, kernel);
            var (code, said) = Slangc.Compile(source, "spirv-asm", output);
            Assert.True(code == 0, $"to SPIR-V assembly exited {code}:\n{said}");
            var arrays = new HashSet<string>();
            var pointers = new HashSet<string>();
            var held = new List<string>();
            foreach (var line in File.ReadLines(output))
            {
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 4 || words[1] != "=")
                    continue;
                if (words[2] == "OpTypeArray" || (words[2] == "OpTypeStruct" && words.Skip(3).Any(arrays.Contains)))
                    arrays.Add(words[0]);
                else if (words[2] == "OpTypePointer" && words.Length > 4 && (words[3] is "Function" or "Private") && arrays.Contains(words[4]))
                    pointers.Add(words[0]);
                else if (words[2] == "OpVariable" && pointers.Contains(words[3]))
                    held.Add(line.Trim());
            }
            return held;
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void Every_File_Of_The_Library_Is_Imported_By_A_Kernel_Of_The_Tests()
    {
        var kernels = Directory.EnumerateFiles(Repository.TestKernels, "*.slang")
            .Select(File.ReadAllText)
            .ToList();
        var library = Directory.EnumerateFiles(Repository.Shaders, "*.slang").ToList();
        Assert.NotEmpty(library);
        var unread = library
            .Select(Path.GetFileNameWithoutExtension)
            .Where(module => !kernels.Any(kernel => kernel.Contains($"import {module};", StringComparison.Ordinal)))
            .ToList();
        Assert.True(unread.Count == 0, "No kernel of the tests imports " + string.Join(", ", unread));
    }
}
