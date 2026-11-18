using System.Reflection;
using Tools;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The checks every module keeps, from the Tools submodule, that the library references no engine,
/// window or graphics API, names no engine in a public member or its documentation nor in any
/// tracked text, has every public type named in the README, holds the README's binding block in its
/// host word for word, keeps the cheatsheet the built library writes, and opens the README with its
/// link row.
/// </summary>
public sealed class ModuleTests
{
    // The library's assembly, the checkout's root and the source of the host that runs the
    // README's binding block, the three a module sets.
    private const string Library = "3DEngine.RadianceCascades";
    private const string Host = "RadianceCascades.Tests/BindingTests.cs";
    private static string Root => Repository.Root;

    private static readonly string Deps = Path.Combine(AppContext.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".deps.json");
    private static readonly string Readme = File.ReadAllText(Path.Combine(Root, "README.md"));

    [Fact]
    public void The_Library_Takes_No_Engine_Window_Or_Graphics_Api()
    {
        var fault = ModuleChecks.ReferenceFault(Assembly.Load(Library), Deps);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void No_Public_Name_Or_Its_Documentation_Names_An_Engine()
    {
        var fault = ModuleChecks.EngineNameFault(Assembly.Load(Library));
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readme_Names_Every_Public_Type()
    {
        var fault = ModuleChecks.UnnamedFault(Assembly.Load(Library), Readme);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readmes_Binding_Block_Stands_Word_For_Word_In_The_Host()
    {
        var fault = ModuleChecks.BindingBlock(Readme, File.ReadAllText(Path.Combine(Root, Host)));
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void No_Tracked_Text_Names_An_Engine()
    {
        var fault = ModuleChecks.EngineNameInTextFault(Root);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readme_Opens_With_Its_Link_Row()
    {
        var fault = ModuleChecks.LinkRowFault(Assembly.Load(Library), Readme);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Cheatsheet_Is_What_The_Built_Library_Writes()
    {
        var fault = ModuleChecks.CheatsheetFault(Assembly.Load(Library), Readme, Path.Combine(Root, "CHEATSHEET.md"));
        Assert.True(fault is null, fault);
    }
}
