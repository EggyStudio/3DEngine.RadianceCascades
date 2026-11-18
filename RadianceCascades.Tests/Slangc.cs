using System.Diagnostics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The Slang compiler the tests compile the library's Slang with, and a compile's outcome.
/// </summary>
internal static class Slangc
{
    /// <summary>
    /// The variable naming a slangc to use in place of the one the tests look for.
    /// </summary>
    internal const string Variable = "RADIANCECASCADES_SLANGC";

    /// <summary>Why a test that compiles Slang is skipped on a machine with no slangc.</summary>
    internal const string Missing =
        "needs slangc, which build/fetch-slang.sh puts under build/tools, or " + Variable + " naming one";

    /// <summary>
    /// The compiler's path, the one <see cref="Variable"/> names, else the first
    /// build/tools/slang/bin/slangc in the tests' folder or a folder above it, else none.
    /// </summary>
    /// <remarks>
    /// The search goes past the repository's root, so a checkout of the module as an engine's
    /// submodule finds the engine's slangc where it has none of its own.
    /// </remarks>
    internal static string? Path { get; } = Find();

    private static string? Find()
    {
        var named = Environment.GetEnvironmentVariable(Variable);
        if (!string.IsNullOrEmpty(named))
            return File.Exists(named) ? named : null;
        var name = OperatingSystem.IsWindows() ? "slangc.exe" : "slangc";
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            var candidate = System.IO.Path.Combine(at.FullName, "build", "tools", "slang", "bin", name);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>
    /// Compiles the compute entry <c>test</c> of <paramref name="kernel"/> for
    /// <paramref name="target"/> into <paramref name="output"/>, the library's Slang on the import
    /// path and every warning an error, as the build's are.
    /// </summary>
    /// <returns>The compiler's exit code and what it wrote to either stream.</returns>
    internal static (int Code, string Said) Compile(string kernel, string target, string output)
    {
        var start = new ProcessStartInfo(Path ?? throw new InvalidOperationException(Missing))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[]
                 {
                     kernel, "-I", Repository.Shaders, "-entry", "test", "-stage", "compute", "-target", target, "-o", output,
                     "-warnings-as-errors", "all",
                 })
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"slangc at {Path} did not start");
        var said = process.StandardError.ReadToEndAsync();
        var written = process.StandardOutput.ReadToEndAsync();
        // A compile of one kernel takes under a second, so two minutes is reached only by a hang.
        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            process.Kill(entireProcessTree: true);
            return (-1, $"slangc ran past two minutes on {kernel}");
        }
        return (process.ExitCode, (written.Result + said.Result).Trim());
    }
}

/// <summary>
/// A theory that compiles Slang, skipped with its reason where there is no slangc.
/// </summary>
internal sealed class SlangTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Marks the theory skipped where <see cref="Slangc.Path"/> found no compiler.
    /// </summary>
    public SlangTheoryAttribute()
    {
        if (Slangc.Path is null)
            Skip = Slangc.Missing;
    }
}
