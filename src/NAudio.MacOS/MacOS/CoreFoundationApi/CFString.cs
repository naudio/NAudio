
/*
    This wrapper was created from the CFString.h header file:

    CFString.h
	Copyright (c) 2006-2019, Apple Inc. and the Swift project authors
 
	Portions Copyright (c) 2014-2019, Apple Inc. and the Swift project authors
	Licensed under Apache License v2.0 with Runtime Library Exception
	See http://swift.org/LICENSE.txt for license information
	See http://swift.org/CONTRIBUTORS.txt for the list of Swift project authors
*/

using System;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// <see cref="CFString" /> provides a suite of efficient string-manipulation and string-conversion functions. 
/// It offers seamless Unicode support and facilitates the sharing of data between Cocoa and C-based programs.
/// <see cref="CFString" /> objects are immutable—use CFMutableStringRef to create and manage a string that can be changed after it has been created. <br /> <br />
/// 
/// <see cref="CFString" /> has two primitive properties, <see cref="Length"/> and <see cref="this[int]"/>, that provide the basis for all other functions in its interface. 
/// The <see cref="Length"/> property returns the total number (in terms of UTF-16 code pairs) of characters in the string. 
/// The <see cref="this[int]"/> property gives access to each character in the string by index, with index values starting at 0. <br /> <br />
/// 
/// <see cref="CFString" /> provides functions for finding and comparing strings. 
/// It also provides functions for reading numeric values from strings, for combining strings in various ways, and for converting a string to different forms (such as encoding and case changes).
/// A number of functions, for example CFStringFindWithOptions, allow you to specify a range over which to operate within a string. 
/// The specified range must not exceed the length of the string. 
/// Debugging options may help you to catch any errors that arise if a range does exceed a string’s length. <br /> <br />
/// 
/// Like other Core Foundation types, you can hash <see cref="CFString" />s using the <see cref="AbstractCoreFoundationObject.GetHashCode"/> method. 
/// You should never, though, store a hash value outside of your application and expect it to be useful if you read it back in later (hash values may change between different releases of the operating system). <br /> <br />
/// 
/// <see cref="CFString" /> is “toll-free bridged” with its Cocoa Foundation counterpart, NSString. 
/// This means that the Core Foundation type is interchangeable in function or method calls with the bridged Foundation object. 
/// Therefore, in a method where you see an NSString * parameter, you can pass in a CFStringRef, and in a function where you see a CFStringRef parameter, you can pass in an NSString instance. 
/// This also applies to concrete subclasses of NSString. 
/// See Toll-Free Bridged Types for more information on toll-free bridging.
/// </summary>
internal sealed class CFString : AbstractCoreFoundationObject, ICloneable
{
    public CFString(nint handle) : base(handle) { }

    public CFString(nint handle, bool addReference) : base(handle, addReference) { }

    public static CFString CreateFromString(string dotnetString)
    {
        dotnetString ??= string.Empty;
        using CFAllocator allocator = CFAllocator.GetDefault();
        return new(NativeMethods.CFStringCreateWithCharacters(
                allocator.DangerousGetObject(),
                dotnetString,
                new System.Runtime.InteropServices.CLong(dotnetString.Length)
        ));
    }

    /// <summary>
    /// Gets a character of the string at the specified position.
    /// </summary>
    /// <param name="index">The index of the character to retrieve.</param>
    /// <returns>The character at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or exceeds the string's bounds.</exception>
    public char this[int index] => (uint)index >= Length
                ? throw new ArgumentOutOfRangeException(nameof(index), "Index cannot exceed the string's bounds.")
                : unchecked((char)NativeMethods.CFStringGetCharacterAtIndex(handle, new(index)));

    /// <summary>
    /// Gets the length, in UTF-16 characters, of this CFString object.
    /// </summary>
    public int Length
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return NativeMethods.CFStringGetLength(handle).Value.ToInt32();
        }
    }

    /// <summary>
    /// Returns a .NET string representing the contents of this CFString object. <br />
    /// Returns <see cref="string.Empty"/> if this object has been disposed of.
    /// </summary>
    /// <returns>The contents of this CFString object as a .NET string.</returns>
    public override unsafe string ToString()
    {
        if (IsInvalid || IsClosed)
        {
            return string.Empty;
        }
        else
        {
            char* pAllocatedString = NativeMethods.CFStringGetCharactersPtr(handle);
            if (pAllocatedString is null)
            {
                string returnValue = new('\0', Length);
                fixed (char* retValPointer = returnValue)
                {
                    NativeMethods.CFStringGetCharacters(handle, new CFRange(0, returnValue.Length), retValPointer);
                }
                return returnValue;
            }
            else
            {
                return new(pAllocatedString, 0, Length);
            }
        }
    }

    public CFString Clone()
    {
        ThrowIfInvalidOrDisposed();
        using CFAllocator allocator = GetAllocator();
        return Clone(allocator);
    }

    public CFString Clone(CFAllocator allocator)
    {
        ThrowIfInvalidOrDisposed();
        ArgumentNullException.ThrowIfNull(allocator);
        return new CFString(
            NativeMethods.CFStringCreateCopy(allocator.NativeObject, handle)
        );
    }

    object ICloneable.Clone() => Clone();
}