using System.Text.RegularExpressions;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The repository's sources held to the width its prose is wrapped at (NORM.md, N 4.11), the C# and
/// the Slang of the library and the tests, the scripts under build and the workflows.
/// </summary>
public sealed partial class SourceTests
{
    private const int Width = 100;

    // The folders read, each with its files and the mark a comment line opens with in them.
    private static readonly (string Folder, string Pattern, string Mark)[] Sources =
    [
        ("RadianceCascades", "*.cs", "//"),
        ("RadianceCascades", "*.slang", "//"),
        ("RadianceCascades.Tests", "*.cs", "//"),
        ("RadianceCascades.Tests", "*.slang", "//"),
        ("build", "*.sh", "#"),
        (Path.Combine(".github", "workflows"), "*.yml", "#"),
    ];

    // A comment line that holds an address and nothing else, which no wrapping shortens.
    [GeneratedRegex(@"^\s*(///?|#)\s*\S+://\S+\s*$")]
    private static partial Regex AddressAlone();

    [Fact]
    public void Every_Comment_Line_Holds_To_A_Hundred_Columns()
    {
        var root = Repository.Root;
        var over = new List<string>();
        foreach (var (folder, pattern, mark) in Sources)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), pattern, SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                var parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj") || parts.Contains("tools"))
                    continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith(mark, StringComparison.Ordinal) && line.Length > Width && !AddressAlone().IsMatch(line))
                        over.Add($"{relative}:{i + 1} ({line.Length})");
                }
            }
        }

        Assert.True(over.Count == 0, $"{over.Count} comment lines past {Width} columns:\n" + string.Join("\n", over));
    }
}
