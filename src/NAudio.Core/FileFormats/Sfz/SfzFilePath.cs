using System;
using System.IO;

namespace NAudio.Sfz;

/// <summary>
/// Turns a file path written inside an SFZ file (an <c>#include</c> target or a
/// <c>sample=</c> path) into a file on disk, applying the rules that stop an
/// untrusted SFZ file from reaching files it has no business opening.
/// </summary>
/// <remarks>
/// <para>
/// Relative paths, including ones that climb out with <c>..</c> (as in the common
/// <c>sample=..\Samples\c4.wav</c> library layout), resolve against the base directory.
/// Absolute paths, UNC/network paths (<c>\\server\share\x.wav</c>) and device paths are
/// refused unless a root directory is given and they fall inside it: on Windows, opening a
/// UNC path written into a file can leak the user's network credentials to whoever runs
/// that server.
/// </para>
/// <para>
/// With a root directory, every path must resolve inside it, which sandboxes a
/// file from an untrusted source to a directory the caller chooses. Symbolic links inside
/// the root are not followed for this check.
/// </para>
/// <para>
/// Empty files and things that aren't regular files (devices, pipes, <c>/proc</c> entries
/// all report a length of zero) are treated as missing, so a path such as
/// <c>../../../dev/zero</c> can't make a reader block or read forever.
/// </para>
/// </remarks>
public static class SfzFilePath
{
    /// <summary>
    /// Resolves <paramref name="path"/> as written in an SFZ file.
    /// </summary>
    /// <param name="baseDirectory">Directory relative paths resolve against (normally the
    /// directory of the .sfz file). Null or empty means the current directory.</param>
    /// <param name="path">The path as written in the SFZ; either separator is accepted.</param>
    /// <param name="rootDirectory">If not null, the resolved path must lie inside this
    /// directory, and absolute paths are allowed only when they do.</param>
    /// <returns>The full path of an existing, non-empty file, or null if the path is
    /// refused or there is no such file.</returns>
    public static string? Resolve(string? baseDirectory, string? path, string? rootDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // SFZ paths use backslashes by convention; accept either separator
        var normalised = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);

        string full;
        try
        {
            // IsPathRooted also catches drive-relative ("C:x"), UNC and \\?\ device paths
            bool rooted = Path.IsPathRooted(normalised);
            if (rooted && rootDirectory == null) return null;
            full = Path.GetFullPath(rooted ? normalised : Path.Combine(baseDirectory ?? "", normalised));

            if (rootDirectory != null && !IsInside(full, Path.GetFullPath(rootDirectory)))
                return null;

            var file = new FileInfo(full);
            return file.Exists && file.Length > 0 ? full : null;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or IOException
                                      or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // malformed or inaccessible path: treat like a missing file
            return null;
        }
    }

    private static bool IsInside(string fullPath, string root)
    {
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!Path.EndsInDirectorySeparator(root)) root += Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, comparison);
    }
}
