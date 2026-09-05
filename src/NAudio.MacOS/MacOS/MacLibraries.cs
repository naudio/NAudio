namespace NAudio.MacOS;

/// <summary>
/// Contains library names for macOS wrappers.
/// </summary>
/// <remarks>
/// Library resolution and loading can be a bit problematic in 
/// macOS if you do not know where the libraries are and how Apple 
/// conceptualizes the entire library scheme. <br />
/// Most of the libraries that provide native API's for macOS are called <c>frameworks</c>, 
/// and most of them are located in /System/Library/Frameworks . <br />
/// Additionally, those framework libraries are 'internally' versionable and each version of
/// a framework library is placed on a subfolder with a single english alphabet letter (e.g. A, B or C).
/// However, macOS provides a symbolic link to the latest version of each library, so that one is going to be used for 
/// resolving the latest framework library version to use. <br />
/// There are notable exceptions to this rule, such as the low-level Darwin 
/// kernel and support API's, but those are following Unix and BSD-like conventions.
/// </remarks>
internal static class MacLibraries
{
    // Base path for all the macOS framework libraries. 
    // They are placed to the following directory path.
    private const string SystemLibraryFrameworks = "/System/Library/Frameworks";

    /// <summary>
    /// Provides the Core Foundation framework library path.
    /// </summary>
    public const string CoreFoundation = $"{SystemLibraryFrameworks}/CoreFoundation.framework/CoreFoundation";

    /// <summary>
    /// Provides the Core Audio framework library path.
    /// </summary>
    public const string CoreAudio = $"{SystemLibraryFrameworks}/CoreAudio.framework/CoreAudio";

    /// <summary>
    /// Provides the Audio Toolbox framework library path.
    /// </summary>
    public const string AudioToolbox = $"{SystemLibraryFrameworks}/AudioToolbox.framework/AudioToolbox";
}