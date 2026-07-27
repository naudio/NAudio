
/*
    This wrapper was created from the CFURL.h header file:

    CFURL.h
	Copyright (c) 2006-2019, Apple Inc. and the Swift project authors
 
	Portions Copyright (c) 2014-2019, Apple Inc. and the Swift project authors
	Licensed under Apache License v2.0 with Runtime Library Exception
	See http://swift.org/LICENSE.txt for license information
	See http://swift.org/CONTRIBUTORS.txt for the list of Swift project authors
*/


using System;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// The <see cref="CFURL" /> opaque type provides facilities for creating, parsing, and dereferencing URL strings. 
/// <see cref="CFURL" /> is useful to applications that need to use URLs to access resources, including local files. <br /> <br />
/// 
/// A <see cref="CFURL" /> object is composed of two parts—a base URL, which can be NULL, and a string that is resolved relative to the base URL. 
/// A <see cref="CFURL" /> object whose string is fully resolved without a base URL is considered absolute; all others are considered relative. <br /> <br />
/// 
/// <see cref="CFURL" /> is “toll-free bridged” with its Cocoa Foundation counterpart, NSURL. This means that the Core Foundation type is interchangeable in function or method calls with the bridged Foundation object. 
/// In other words, in a method where you see an NSURL * parameter, you can pass in a CFURLRef, and in a function where you see a CFURLRef parameter, you can pass in an NSURL instance.
/// This also applies to concrete subclasses of NSURL. 
/// See Toll-Free Bridged Types for more information on toll-free bridging.
/// </summary>
internal sealed class CFURL : AbstractCoreFoundationObject
{
    public CFURL(nint handle) : base(handle) { }

    public CFURL(nint handle, bool addReference) : base(handle, addReference) { }

    [return: NotNull]
    public static CFURL CreateFromUri(Uri uri) => CreateFromUri(uri, null);

    [return: NotNull]
    public static CFURL CreateFromUri(Uri uri, CFURL baseURL)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return CreateFromString(uri.ToString(), baseURL);
    }

    [return: NotNull]
    public static CFURL CreateFromString(string uri) => CreateFromString(uri, null);

    [return: NotNull]
    public static CFURL CreateFromString(string uri, CFURL baseURL)
    {
        ArgumentNullException.ThrowIfNull(uri);
        using CFString c = CFString.CreateFromString(uri);
        using CFAllocator allc = CFAllocator.GetDefault();
        return new(NativeMethods.CFURLCreateWithString(
                allc.DangerousGetObject(),
                c.DangerousGetObject(),
                baseURL is null ? IntPtr.Zero : baseURL.NativeObject
        ));
    }

    public static CFURL CreateFromFilePath(string filePath, bool isDir)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        using CFString c = CFString.CreateFromString(filePath);
        using CFAllocator allc = CFAllocator.GetDefault();
        return new(NativeMethods.CFURLCreateWithFileSystemPath(
            allc.DangerousGetObject(),
            c.DangerousGetObject(),
            CFURLPathStyle.kCFURLPOSIXPathStyle,
            isDir ? MacBoolean.True : MacBoolean.False
        ));
    }

    /// <summary>Gets the base URL of this URL object, if any.</summary>
    /// <returns>A new <see cref="CFURL"/> instance containing the base URL.</returns>
    [return: MaybeNull]
    public CFURL GetBaseURL()
    {
        IntPtr h = NativeMethods.CFURLGetBaseURL(handle);
        return h == IntPtr.Zero ? null : new CFURL(h, true);
    }

    /// <summary>Returns the URL as a <see cref="Uri"/> object.</summary>
    /// <returns>A <see cref="Uri"/> representation of this URL instance.</returns>
    [return: NotNull]
    public Uri ToUri() => new(ToString());

    /// <summary>Returns the URL as a <see cref="string"/> object.</summary>
    /// <returns>A string representation of this URL instance.</returns>
    [return: NotNull]
    public override string ToString()
    {
        if (IsClosed || IsInvalid)
        {
            return string.Empty;
        }
        else
        {
            using CFString c = new(NativeMethods.CFURLGetString(handle), true);
            return c.ToString();
        }
    }
}