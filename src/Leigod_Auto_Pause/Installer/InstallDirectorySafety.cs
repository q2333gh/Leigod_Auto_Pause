namespace Leigod_Auto_Pause.Installer;

public static class InstallDirectorySafety
{
    private static readonly string[] RequiredRelativePaths =
    [
        Path.Combine("resources", "app.asar"),
        "leigod_launcher.exe"
    ];

    public static bool IsSafeInstallDirectory(string? directoryPath, Func<string, bool>? fileExists = null)
    {
        return TryValidateInstallDirectory(directoryPath, fileExists, out _, out _);
    }

    public static string EnsureSafeInstallDirectory(string? directoryPath, Func<string, bool>? fileExists = null)
    {
        if (!TryValidateInstallDirectory(directoryPath, fileExists, out var normalizedPath, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalizedPath;
    }

    public static bool TryValidateInstallDirectory(
        string? directoryPath,
        Func<string, bool>? fileExists,
        out string normalizedPath,
        out string errorMessage)
    {
        normalizedPath = string.Empty;
        errorMessage = "未识别为有效的雷神安装目录，为避免误操作已停止。";

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            errorMessage = "安装目录为空，为避免误操作已停止。";
            return false;
        }

        try
        {
            normalizedPath = NormalizeDirectoryPath(directoryPath);
        }
        catch (Exception)
        {
            errorMessage = "安装目录格式无效，为避免误操作已停止。";
            return false;
        }

        if (IsRejectedBroadDirectory(normalizedPath))
        {
            errorMessage = $"目录 '{normalizedPath}' 范围过大，为避免误操作已停止。";
            return false;
        }

        var pathExists = fileExists ?? File.Exists;
        var validatedPath = normalizedPath;
        if (!RequiredRelativePaths.All(relativePath => pathExists(Path.Combine(validatedPath, relativePath))))
        {
            errorMessage = $"目录 '{normalizedPath}' 不包含完整的雷神安装标记，为避免误操作已停止。";
            return false;
        }

        return true;
    }

    public static string NormalizeDirectoryPath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsRejectedBroadDirectory(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return true;
        }

        var pathRoot = Path.GetPathRoot(normalizedPath);
        if (!string.IsNullOrWhiteSpace(pathRoot) &&
            string.Equals(NormalizeDirectoryPath(pathRoot), normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return GetBroadDirectories()
            .Any(candidate => string.Equals(candidate, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetBroadDirectories()
    {
        foreach (var candidate in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        })
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                yield return NormalizeDirectoryPath(candidate);
            }
        }
    }
}
