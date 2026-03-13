using Leigod_Auto_Pause.Installer;

namespace Leigod_Auto_Pause.Tests.Installer;

public class InstallDirectorySafetyTests
{
    [Fact]
    public void TryValidateInstallDirectory_WhenMarkersPresent_ReturnsNormalizedPath()
    {
        var isValid = InstallDirectorySafety.TryValidateInstallDirectory(
            @"D:\Leigod\\",
            path => path is @"D:\Leigod\resources\app.asar" or @"D:\Leigod\leigod_launcher.exe",
            out var normalizedPath,
            out _);

        Assert.True(isValid);
        Assert.Equal(@"D:\Leigod", normalizedPath);
    }

    [Fact]
    public void TryValidateInstallDirectory_WhenDirectoryIsBroadLocation_ReturnsFalse()
    {
        var isValid = InstallDirectorySafety.TryValidateInstallDirectory(
            @"C:\",
            _ => true,
            out _,
            out var errorMessage);

        Assert.False(isValid);
        Assert.Contains("范围过大", errorMessage);
    }

    [Fact]
    public void TryValidateInstallDirectory_WhenMarkersMissing_ReturnsFalse()
    {
        var isValid = InstallDirectorySafety.TryValidateInstallDirectory(
            @"D:\Somewhere",
            _ => false,
            out _,
            out var errorMessage);

        Assert.False(isValid);
        Assert.Contains("安装标记", errorMessage);
    }
}
