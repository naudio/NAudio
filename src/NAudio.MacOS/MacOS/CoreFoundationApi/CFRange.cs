/*
    Acquired from the CFBase.h native header at 5/6/2026:

    CFBase.h
	Copyright (c) 1998-2019, Apple Inc. and the Swift project authors
 
	Portions Copyright (c) 2014-2019, Apple Inc. and the Swift project authors
	Licensed under Apache License v2.0 with Runtime Library Exception
	See http://swift.org/LICENSE.txt for license information
	See http://swift.org/CONTRIBUTORS.txt for the list of Swift project authors
*/

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