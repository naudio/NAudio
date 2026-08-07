// This interop definition was derived from the file CFBase.h of the Core Foundation Framework.
// See https://developer.apple.com/documentation/corefoundation for more information.

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// A structure representing a range of sequential items in a container, such as characters in a buffer or elements in a collection.
/// </summary>
public readonly struct CFRange
{
    /// <summary>
    /// An integer representing the starting location of the range. <br />
    /// For type compatibility with the rest of the system, <see cref="int.MaxValue"/> is the maximum value you should use for location.
    /// </summary>
    public readonly CFIndex location;
    /// <summary>
    /// An integer representing the number of items in the range. <br />
    /// For type compatibility with the rest of the system, <see cref="int.MaxValue"/> is the maximum value you should use for length.
    /// </summary>
    public readonly CFIndex length;

    /// <summary>
    /// Initializes a CFRange structure. <br />
    /// This corresponds to the <c>CFRangeMake</c> function in native code.
    /// </summary>
    /// <param name="location">The starting location of the range.</param>
    /// <param name="length">The length of the range.</param>
    public CFRange(int location, int length)
    {
        this.length = new CFIndex(length);
        this.location = new CFIndex(location);
    }

    /// <summary>
    /// Initializes a CFRange structure. <br />
    /// This corresponds to the <c>CFRangeMake</c> function in native code.
    /// </summary>
    /// <param name="location">The starting location of the range.</param>
    /// <param name="length">The length of the range.</param>
    public CFRange(long location, long length)
    {
        this.length = new CFIndex(new nint(length));
        this.location = new CFIndex(new nint(location));
    }
}