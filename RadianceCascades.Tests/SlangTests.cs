using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The library's Slang compiled to the two targets the engines build it for, SPIR-V for 3DEngine's
/// Vulkan and WGSL for BevyCSharp's, by the tests' kernels that import it, with no GPU.
/// </summary>
/// <remarks>
/// A file of the library that no kernel calls would be checked by the compiler's parser alone, and
/// a function that one target cannot generate shows only when a kernel calls it, so every file is
/// imported by a kernel of the tests that calls each of its functions.
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
    public void A_Kernel_Of_The_Library_Compiles_To_SpirV_And_To_Wgsl_With_No_Warning(string kernel)
    {
        var folder = Directory.CreateTempSubdirectory("radiancecascades-slang-");
        try
        {
            var source = Path.Combine(Repository.TestKernels, kernel);
            foreach (var (target, extension) in new[] { ("spirv", "spv"), ("wgsl", "wgsl") })
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
