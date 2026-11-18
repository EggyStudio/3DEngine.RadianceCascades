namespace RadianceCascades.Tests;

/// <summary>
/// The repository's folders as the tests read them, found from the tests' own folder.
/// </summary>
internal static class Repository
{
    /// <summary>
    /// The repository's root, the folder above the tests that holds the solution.
    /// </summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>
    /// The library's Slang, which an engine adds to the folders its slangc imports from.
    /// </summary>
    internal static string Shaders => Path.Combine(Root, "RadianceCascades", "Shaders");

    /// <summary>
    /// The tests' own kernels, each importing the library's Slang and calling it.
    /// </summary>
    internal static string TestKernels => Path.Combine(Root, "RadianceCascades.Tests", "Shaders");

    private static string FindRoot()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "RadianceCascades.slnx")))
            at = at.Parent;
        return at?.FullName
            ?? throw new DirectoryNotFoundException("No RadianceCascades.slnx above the tests.");
    }
}
