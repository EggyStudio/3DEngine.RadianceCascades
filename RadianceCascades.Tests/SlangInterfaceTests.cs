using System.Text.RegularExpressions;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The interfaces the library's Slang declares for an engine's structs to implement, each named in
/// the README's table of Slang interfaces, which is where an engine binding the module finds what
/// its kernels give the library's functions.
/// </summary>
public sealed partial class SlangInterfaceTests
{
    [GeneratedRegex(@"public\s+interface\s+(\w+)")]
    private static partial Regex Declared();

    [GeneratedRegex(@"^\| `(\w+)` \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    [Fact]
    public void Every_Interface_Of_The_Librarys_Slang_Has_A_Row_In_The_Readmes_Table()
    {
        var readme = File.ReadAllText(Path.Combine(Repository.Root, "README.md"));
        var head = readme.IndexOf("| Slang interface |", StringComparison.Ordinal);
        Assert.True(head >= 0, "the README has no table of Slang interfaces");
        // The table runs from its head to the first line that is not a row of it.
        var end = readme.IndexOf("\n\n", head, StringComparison.Ordinal);
        var rows = Row().Matches(readme[head..(end < 0 ? readme.Length : end)]).Select(m => m.Groups[1].Value).ToHashSet();
        var unnamed = Directory.EnumerateFiles(Repository.Shaders, "*.slang")
            .SelectMany(file => Declared().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .Where(name => !rows.Contains(name))
            .Order()
            .ToList();
        Assert.True(unnamed.Count == 0, "The README's table of Slang interfaces has no row for " + string.Join(", ", unnamed));
    }
}
