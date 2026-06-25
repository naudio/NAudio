using System.IO;

namespace NAudio.Sfz;

/// <summary>
/// Resolves <c>#include "file.sfz"</c> directives to their text. The default
/// <see cref="FileSfzIncludeResolver"/> reads from disk relative to the
/// including file; tests (or virtual file systems) can supply their own.
/// </summary>
public interface ISfzIncludeResolver
{
    /// <summary>
    /// Returns the text of an included file, or null if it cannot be found.
    /// <paramref name="path"/> is the include path as written in the file.
    /// </summary>
    string Resolve(string path);
}

/// <summary>
/// An <see cref="ISfzIncludeResolver"/> that reads includes from the file
/// system, resolving relative paths against a base directory.
/// </summary>
public sealed class FileSfzIncludeResolver : ISfzIncludeResolver
{
    private readonly string baseDirectory;

    /// <summary>Creates a resolver rooted at the given base directory.</summary>
    public FileSfzIncludeResolver(string baseDirectory)
    {
        this.baseDirectory = baseDirectory ?? "";
    }

    /// <inheritdoc />
    public string Resolve(string path)
    {
        // SFZ paths use backslashes by convention; accept either separator
        var normalised = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);
        var full = Path.IsPathRooted(normalised)
            ? normalised
            : Path.Combine(baseDirectory, normalised);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }
}
