using AsarSharp;

namespace Leigod_Auto_Pause.Tests.Packaging;

public class ArchivePathSafetyTests
{
    [Fact]
    public void CombineWithinRoot_WhenRelativePathStaysInsideRoot_ReturnsCombinedPath()
    {
        var combinedPath = ArchivePathSafety.CombineWithinRoot(@"C:\temp\asar", @"dist\main\main.js");

        Assert.Equal(@"C:\temp\asar\dist\main\main.js", combinedPath);
    }

    [Fact]
    public void CombineWithinRoot_WhenRelativePathEscapesRoot_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ArchivePathSafety.CombineWithinRoot(@"C:\temp\asar", @"..\..\outside.txt"));
    }

    [Fact]
    public void EnsurePathWithinRoot_WhenAbsolutePathEscapesRoot_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ArchivePathSafety.EnsurePathWithinRoot(@"C:\temp\asar", @"C:\temp\outside.txt"));
    }
}
