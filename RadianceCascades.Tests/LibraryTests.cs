using System.Reflection;
using Xunit;

namespace RadianceCascades.Tests;

public class LibraryTests
{
    [Fact]
    public void The_Library_Loads()
    {
        // By its name rather than by a type of its own, so the test reads that the project
        // reference puts the assembly beside the tests under the name a package of it takes.
        var library = Assembly.Load("3DEngine.RadianceCascades");
        Assert.Equal("3DEngine.RadianceCascades", library.GetName().Name);
    }
}
