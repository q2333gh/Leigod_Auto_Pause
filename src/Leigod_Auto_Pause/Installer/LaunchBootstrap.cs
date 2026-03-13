namespace Leigod_Auto_Pause.Installer;

public sealed class LaunchBootstrap
{
    private readonly Func<string, bool> _fileExists;

    public LaunchBootstrap(Func<string, bool>? fileExists = null)
    {
        _fileExists = fileExists ?? File.Exists;
    }

    public LaunchBootstrapResult Decide(
        string executablePath,
        string currentDirectory,
        IReadOnlyList<LeigodInstallCandidate> candidates)
    {
        if (InstallDirectorySafety.TryValidateInstallDirectory(currentDirectory, _fileExists, out var normalizedCurrentDirectory, out _))
        {
            return new LaunchBootstrapResult(BootstrapAction.RunInPlace, normalizedCurrentDirectory, executablePath, null);
        }

        var validCandidates = candidates
            .Select(candidate =>
            {
                var isValid = InstallDirectorySafety.TryValidateInstallDirectory(candidate.DirectoryPath, _fileExists, out var normalizedPath, out _);
                return new
                {
                    Candidate = candidate,
                    IsValid = isValid,
                    NormalizedPath = normalizedPath
                };
            })
            .Where(x => x.IsValid)
            .GroupBy(x => x.NormalizedPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LeigodInstallCandidate(group.Key, group.Max(x => x.Candidate.Score)))
            .OrderByDescending(candidate => candidate.Score)
            .ToList();

        if (validCandidates.Count == 0)
        {
            return new LaunchBootstrapResult(BootstrapAction.Abort, null, null, "未找到可安全操作的雷神安装目录。");
        }

        var highestScore = validCandidates[0].Score;
        var topCandidates = validCandidates
            .Where(candidate => candidate.Score == highestScore)
            .ToList();

        if (topCandidates.Count > 1)
        {
            return new LaunchBootstrapResult(BootstrapAction.Abort, null, null, "检测到多个同等可信的雷神安装目录，为避免误操作已停止。");
        }

        var targetDirectory = topCandidates[0].DirectoryPath;
        var installedExecutablePath = Path.Combine(targetDirectory, SelfInstaller.InstalledFileName);
        return new LaunchBootstrapResult(BootstrapAction.InstallAndRelaunch, targetDirectory, installedExecutablePath, null);
    }
}
