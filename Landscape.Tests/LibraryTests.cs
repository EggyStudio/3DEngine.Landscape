using System.Reflection;
using Xunit;

namespace Landscape.Tests;

public class LibraryTests
{
    [Fact]
    public void The_Library_Loads()
    {
        // By its name rather than by a type of its own, so the test reads that the project
        // reference puts the assembly beside the tests under the name a package carries.
        var library = Assembly.Load("3DEngine.Landscape");
        Assert.Equal("3DEngine.Landscape", library.GetName().Name);
        Assert.Same(library, typeof(Heightfield).Assembly);
    }
}
