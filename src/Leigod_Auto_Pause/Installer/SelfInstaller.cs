namespace Leigod_Auto_Pause.Installer;

public sealed class SelfInstaller
{
    public const string InstalledFileName = "Leigod_Auto_Pause.exe";
    public const string ShortcutFileName = "雷神自动暂停.lnk";

    private readonly Action<string, string, bool> _copyFile;
    private readonly Action<string> _ensureDirectory;
    private readonly IShortcutService _shortcutService;
    private readonly Func<string, bool> _fileExists;

    public SelfInstaller(
        Action<string, string, bool>? copyFile = null,
        Action<string>? ensureDirectory = null,
        IShortcutService? shortcutService = null,
        Func<string, bool>? fileExists = null)
    {
        _copyFile = copyFile ?? File.Copy;
        _ensureDirectory = ensureDirectory ?? (path => Directory.CreateDirectory(path));
        _shortcutService = shortcutService ?? new DesktopShortcutService();
        _fileExists = fileExists ?? File.Exists;
    }

    public string Install(string sourceExePath, string targetDirectory, string desktopDirectory)
    {
        var normalizedTargetDirectory = InstallDirectorySafety.EnsureSafeInstallDirectory(targetDirectory, _fileExists);
        _ensureDirectory(normalizedTargetDirectory);

        var installedExePath = Path.Combine(normalizedTargetDirectory, InstalledFileName);
        if (!string.Equals(
                Path.GetFullPath(sourceExePath),
                Path.GetFullPath(installedExePath),
                StringComparison.OrdinalIgnoreCase))
        {
            _copyFile(sourceExePath, installedExePath, true);
        }

        var shortcutPath = Path.Combine(Path.GetFullPath(desktopDirectory), ShortcutFileName);
        TryCreateShortcut(shortcutPath, installedExePath, normalizedTargetDirectory);
        return installedExePath;
    }

    private void TryCreateShortcut(string shortcutPath, string installedExePath, string normalizedTargetDirectory)
    {
        try
        {
            _shortcutService.CreateOrUpdate(shortcutPath, installedExePath, normalizedTargetDirectory, string.Empty, installedExePath);
        }
        catch
        {
            // Shortcut creation is a convenience step. Installation should still succeed
            // even when the user's desktop path is redirected or temporarily unavailable.
        }
    }
}
