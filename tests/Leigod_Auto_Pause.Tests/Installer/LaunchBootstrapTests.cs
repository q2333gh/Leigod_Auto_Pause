using Leigod_Auto_Pause.Installer;

namespace Leigod_Auto_Pause.Tests.Installer;

public class LaunchBootstrapTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "LeigodAutoPauseTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Decide_WhenCurrentDirectoryAlreadyContainsLeigodFiles_ReturnsAlreadyInstalled()
    {
        var installDir = Path.Combine(_tempRoot, "Leigod");
        Directory.CreateDirectory(Path.Combine(installDir, "resources"));
        File.WriteAllText(Path.Combine(installDir, "resources", "app.asar"), "asar");
        File.WriteAllText(Path.Combine(installDir, "leigod_launcher.exe"), "launcher");

        var bootstrap = new LaunchBootstrap();
        var result = bootstrap.Decide(
            executablePath: Path.Combine(installDir, "Leigod_Auto_Pause.exe"),
            currentDirectory: installDir,
            candidates: []);

        Assert.Equal(BootstrapAction.RunInPlace, result.Action);
        Assert.Equal(installDir, result.TargetDirectory);
    }

    [Fact]
    public void Decide_WhenOutsideLeigodDirectory_ReturnsInstallAndRelaunch()
    {
        var bootstrap = new LaunchBootstrap(fileExists: path => path is @"D:\Leigod\resources\app.asar" or @"D:\Leigod\leigod_launcher.exe");
        var result = bootstrap.Decide(
            executablePath: @"C:\Users\me\Downloads\Leigod_Auto_Pause.exe",
            currentDirectory: @"C:\Users\me\Downloads",
            candidates:
            [
                new LeigodInstallCandidate(@"D:\Leigod", 100)
            ]);

        Assert.Equal(BootstrapAction.InstallAndRelaunch, result.Action);
        Assert.Equal(@"D:\Leigod\Leigod_Auto_Pause.exe", result.InstalledExecutablePath);
    }

    [Fact]
    public void Decide_WhenTopCandidatesAreAmbiguous_Aborts()
    {
        var bootstrap = new LaunchBootstrap(fileExists: path =>
            path is @"D:\LeigodA\resources\app.asar" or
            @"D:\LeigodA\leigod_launcher.exe" or
            @"E:\LeigodB\resources\app.asar" or
            @"E:\LeigodB\leigod_launcher.exe");

        var result = bootstrap.Decide(
            executablePath: @"C:\Users\me\Downloads\Leigod_Auto_Pause.exe",
            currentDirectory: @"C:\Users\me\Downloads",
            candidates:
            [
                new LeigodInstallCandidate(@"D:\LeigodA", 100),
                new LeigodInstallCandidate(@"E:\LeigodB", 100)
            ]);

        Assert.Equal(BootstrapAction.Abort, result.Action);
        Assert.Contains("多个", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }
}
