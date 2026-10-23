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
    string? Resolve(string path);
}

/// <summary>
/// An <see cref="ISfzIncludeResolver"/> that reads includes from the file
/// system, resolving relative paths against a base directory. Absolute and
/// network paths are refused unless a root directory allows them; see
/// <see cref="SfzFilePath"/> for the rules.
/// </summary>
public sealed class FileSfzIncludeResolver : ISfzIncludeResolver
{
    /// <summary>
    /// Largest include file that will be read. Real include files are a few kilobytes;
    /// this only stops a hostile path pointing the parser at something enormous.
    /// </summary>
    public const long MaxIncludeFileBytes = 16 * 1024 * 1024;

    private readonly string baseDirectory;
    private readonly string? rootDirectory;

    /// <summary>Creates a resolver rooted at the given base directory.</summary>
    public FileSfzIncludeResolver(string? baseDirectory)
        : this(baseDirectory, null)
    {
    }

    /// <summary>
    /// Creates a resolver that resolves relative paths against
    /// <paramref name="baseDirectory"/> and, if <paramref name="rootDirectory"/> is not
    /// null, refuses any include that resolves outside it. Use a root directory to
    /// sandbox SFZ files from untrusted sources.
    /// </summary>
    public FileSfzIncludeResolver(string? baseDirectory, string? rootDirectory)
    {
        this.baseDirectory = baseDirectory ?? "";
        this.rootDirectory = rootDirectory;
    }

    /// <inheritdoc />
    public string? Resolve(string path)
    {
        var full = SfzFilePath.Resolve(baseDirectory, path, rootDirectory);
        if (full == null) return null;
        if (new FileInfo(full).Length > MaxIncludeFileBytes)
            throw new InvalidDataException($"SFZ include '{path}' is larger than {MaxIncludeFileBytes} bytes");
        return File.ReadAllText(full);
    }
}
