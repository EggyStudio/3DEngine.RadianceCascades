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

    // naga, which wgpu reads WGSL through, refuses a function returning a value whose end it does
    // not prove unreached, and the merge's loop of marches, endless with each return inside it,
    // kept one engine's merge from being made until a return followed the loop. slangc writes
    // each WGSL function from a line beginning `fn` to a line of `}` alone.
    [SlangTheory]
    [MemberData(nameof(Kernels))]
    public void A_Kernel_Of_The_Library_Ends_Every_Wgsl_Function_That_Returns_A_Value_On_A_Return(string kernel)
    {
        var open = new List<string>();
        string? function = null, last = null;
        foreach (var line in CompiledOf(File.ReadAllText(Path.Combine(Repository.TestKernels, kernel)), "wgsl"))
        {
            if (line.StartsWith("fn ", StringComparison.Ordinal) && line.Contains("->", StringComparison.Ordinal))
                (function, last) = (line[..line.IndexOf('(')], null);
            else if (function is not null && line == "}")
            {
                if (last is null || !last.TrimStart().StartsWith("return", StringComparison.Ordinal))
                    open.Add(function);
                function = null;
            }
            else if (function is not null && line.Trim().Length > 0)
                last = line;
        }
        Assert.True(open.Count == 0, $"{kernel} has WGSL functions that may end with no return:\n" + string.Join("\n", open));
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

    // An engine's hitLight shades a surface with its lights and shadows, which a driver inlines at
    // every call, and with a call at each of the three ways the march met a surface and a march at
    // each of the merge's three legs, one engine's merge took 2.6 minutes and 3.75 GB to compile.
    // A hitLight that counts its calls atomically leaves one atomic in the SPIR-V for each copy of
    // it, since slangc inlines every call and an atomic is never merged with another. The copy lies
    // outside the loop the march's steps take, in a loop about it that shades once and goes on,
    // since a shading inlined in the step loop is paid for at every step: one engine's trace took
    // 4.02 and 5.50 ms in two scenes so, and 1.75 and 2.02 with the shading after its step loop.
    [SlangTheory]
    [InlineData("traceProbeRay(field, float3(0.0), float3(1.0, 0.0, 0.0), 0.0, read[9]).light.x")]
    [InlineData("mergedLight(field, probes, gridPlaces(far), far, read[9], float3(0.0), 4u, uint2(1u), near, sky, none, marched).x")]
    [InlineData("mergedLight(field, probes, gridPlaces(far), far, read[9], float3(0.0), 4u, uint2(1u), near, sky, none, marched, share).x + share")]
    public void A_Ray_Traced_Or_Merged_Shades_A_Surface_It_Meets_At_One_Place_Outside_The_Loop_Of_Its_Steps(string light)
    {
        var kernel = $$"""
            import rc_cascade_merge;
            RWStructuredBuffer<float> read;
            RWStructuredBuffer<uint> shaded;
            struct CountedField : IProbeField
            {
                int cascadeOf(float3 p, float margin) { return all(abs(p) < 8.0) ? 0 : -1; }
                float4 cornerAndCell(int cascade) { return float4(-8.0, -8.0, -8.0, 1.0); }
                int size() { return 16; }
                float4 between(int cascade, float3 cells) { return float4(read[0], 0.0, 0.0, 0.0); }
                float4 cellAt(int cascade, int3 cell) { return float4(read[1], 0.0, 0.0, 0.0); }
                float3 secondAt(int cascade, int3 cell) { return float3(0.0); }
                float throughAt(int cascade, int3 cell) { return read[2]; }
                float3 hitLight(float3 place, float3 way, int cascade, int3 cell)
                {
                    uint before;
                    InterlockedAdd(shaded[0], 1u, before);
                    return float3(float(before));
                }
            };
            struct OneLight : IFarSkyProbes
            {
                float3 lightAt(int3 probe, uint2 texel) { return float3(read[3]); }
                float reachAt(int3 probe, uint2 texel) { return read[4]; }
                float skyShareAt(int3 probe, uint2 texel) { return read[10]; }
            };
            [numthreads(1, 1, 1)]
            void test(uint3 id : SV_DispatchThreadID)
            {
                CountedField field;
                OneLight probes;
                ProbeCascade far = { float3(-8.0), 2.0, int3(8), 4u, 0.0, read[5], 32u };
                ProbeRay near;
                near.ended = RayPast;
                near.t = read[6];
                near.carried = 1.0;
                near.light = float3(0.0);
                UniformSky sky = { float3(read[7]) };
                NoSteps none;
                bool marched;
                float share;
                read[8] = {{light}};
            }
            """;
        var assembly = CompiledOf(kernel, "spirv-asm");
        var copies = assembly.Count(line => line.Contains(" OpAtomicIAdd ", StringComparison.Ordinal));
        Assert.Equal(1, copies);
        var (around, nested) = LoopsAboutTheAtomic(assembly);
        Assert.True(around > 0 && nested > 0,
            $"the shading lies within {around} loops, the innermost of them holding {nested} loops of its own, where the steps' loop is to lie within it");
    }

    // How many loops of a kernel's SPIR-V hold the block of its one atomic, and how many loops lie
    // within the innermost of them, read from the blocks' branches: a loop is every block its
    // header, the block holding its OpLoopMerge, reaches without passing its merge block or coming
    // back to the header, as SPIR-V's structured control flow lays a loop out.
    private static (int Around, int Nested) LoopsAboutTheAtomic(string[] assembly)
    {
        var next = new Dictionary<string, string[]>();
        var merges = new Dictionary<string, string>();
        string? block = null, atomic = null;
        foreach (var line in assembly)
        {
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 3 && words[1] == "=" && words[2] == "OpLabel")
            {
                block = words[0];
                next[block] = [];
                continue;
            }
            if (block is null || words.Length == 0)
                continue;
            var (op, args) = words[0].StartsWith("Op", StringComparison.Ordinal) ? (words[0], words[1..])
                : words.Length > 2 && words[1] == "=" ? (words[2], words[3..]) : ("", []);
            if (op == "OpLoopMerge")
                merges[block] = args[0];
            else if (op == "OpBranch")
                next[block] = [args[0]];
            else if (op == "OpBranchConditional")
                next[block] = [args[1], args[2]];
            else if (op == "OpSwitch")
                next[block] = args[1..].Where(arg => arg.StartsWith('%')).ToArray();
            else if (op == "OpAtomicIAdd")
                atomic = block;
            else if (op == "OpFunctionEnd")
                block = null;
        }
        var loops = merges.ToDictionary(loop => loop.Key, loop =>
        {
            var held = new HashSet<string> { loop.Key };
            var reached = new Stack<string>(next[loop.Key]);
            while (reached.TryPop(out var at))
                if (at != loop.Value && held.Add(at))
                    foreach (var to in next[at])
                        reached.Push(to);
            return held;
        });
        var around = loops.Where(loop => atomic is not null && loop.Value.Contains(atomic)).OrderBy(loop => loop.Value.Count).ToList();
        if (around.Count == 0)
            return (0, 0);
        var innermost = around[0];
        return (around.Count, loops.Keys.Count(header => header != innermost.Key && innermost.Value.Contains(header)));
    }

    // The lines slangc writes for a kernel to target, SPIR-V assembly or WGSL.
    private static string[] CompiledOf(string kernel, string target)
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-slang-");
        try
        {
            var source = Path.Combine(folder.FullName, "kernel.slang");
            var output = Path.Combine(folder.FullName, target == "wgsl" ? "kernel.wgsl" : "kernel.spvasm");
            File.WriteAllText(source, kernel);
            var (code, said) = Slangc.Compile(source, target, output);
            Assert.True(code == 0, $"to {target} exited {code}:\n{said}");
            return File.ReadAllLines(output);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    // The variables a kernel's SPIR-V keeps in a thread's own memory, its Function and Private
    // storage, that hold an array or a struct with one, read from the assembly slangc writes.
    // SPIR-V is the one target whose assembly names each variable's storage, and slangc lowers
    // every target from the same code.
    private static List<string> ThreadArraysOf(string kernel)
    {
        var arrays = new HashSet<string>();
        var pointers = new HashSet<string>();
        var held = new List<string>();
        foreach (var line in CompiledOf(kernel, "spirv-asm"))
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
