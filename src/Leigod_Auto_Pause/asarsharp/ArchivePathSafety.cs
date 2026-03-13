using System.IO;

namespace AsarSharp;

public static class ArchivePathSafety
{
    public static string NormalizeDirectoryPath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static string CombineWithinRoot(string rootDirectory, string relativePath)
    {
        return EnsurePathWithinRoot(rootDirectory, Path.Combine(NormalizeDirectoryPath(rootDirectory), relativePath));
    }

    public static string EnsurePathWithinRoot(string rootDirectory, string candidatePath)
    {
        var normalizedRoot = NormalizeDirectoryPath(rootDirectory);
        var normalizedCandidate = NormalizeDirectoryPath(candidatePath);

        if (string.Equals(normalizedRoot, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedCandidate;
        }

        var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        var altRootPrefix = normalizedRoot + Path.AltDirectorySeparatorChar;
        if (normalizedCandidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.StartsWith(altRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedCandidate;
        }

        throw new InvalidDataException($"Archive entry path '{candidatePath}' resolves outside the extraction root.");
    }
}
