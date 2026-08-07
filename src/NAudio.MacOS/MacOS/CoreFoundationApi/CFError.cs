// This interop definition was derived from the file CFError.h of the Core Foundation Framework.
// See https://developer.apple.com/documentation/corefoundation for more information.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// A <see cref="CFError" /> object encapsulates more rich and extensible error information than is possible using only an error code or error string. 
/// The core attributes of a <see cref="CFError" /> object are an error domain (represented by a string), a domain-specific error code, and a “user info” dictionary containing application-specific information. 
/// Errors are required to have a domain and an error code within that domain. 
/// Several well-known domains are defined corresponding to Mach, POSIX, and OSStatus errors. <br /> <br />
/// 
/// The optional “user info” dictionary may provide additional information that might be useful for the interpretation and reporting of the error, including a human-readable description for the error.
///  The “user info” dictionary sometimes includes another <see cref="CFError" /> object that represents an error in a subsystem underlying the error represented by the containing <see cref="CFError" /> object. 
/// This underlying error object may provide more specific information about the cause of the error. <br /> <br />
/// 
/// In general, a method should signal an error condition by returning, for example, <see langword="false"/> or <see langword="null"/> rather than by the simple presence of an error object. 
/// The method can then optionally return an <see cref="CFError" /> object by reference, in order to further describe the error. <br /> <br />
/// 
/// <see cref="CFError" /> is toll-free bridged to NSError in the Foundation framework—for more details on toll-free bridging, see Toll-Free Bridged Types. 
/// NSError has some additional guidelines that make it easy to report errors automatically to users and attempt to recover from them. 
/// See Error Handling Programming Guide for more information on NSError programming guidelines.
/// </summary>
internal sealed class CFError : AbstractCoreFoundationObject
{
    public const string PosixDomain = "NSPOSIXErrorDomain";
    public const string OSStatusDomain = "NSOSStatusErrorDomain";

    public CFError(nint handle) : base(handle) { }

    public CFError(nint handle, bool addReference) : base(handle, addReference) { }

    public static CFError Create(CLong errorCode, string errorDomain)
    {
        using CFAllocator allocator = CFAllocator.GetDefault();
        using CFString domainString = CFString.CreateFromString(errorDomain);
        return new(
            NativeMethods.CFErrorCreate(
                allocator.DangerousGetObject(),
                domainString.DangerousGetObject(),
                errorCode
            )
        );
    }

    [NotNull]
    public string ErrorDomain
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            using CFString nativeString = new(
                NativeMethods.CFErrorGetDomain(handle),
                true
            );
            return nativeString.ToString();
        }
    }

    public CLong ErrorCode
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return NativeMethods.CFErrorGetCode(handle);
        }
    }

    [MaybeNull]
    public string FailureReason
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            IntPtr h = NativeMethods.CFErrorCopyFailureReason(handle);
            if (h == IntPtr.Zero)
            {
                return null;
            }
            else
            {
                using CFString nativeString = new(h);
                return nativeString.ToString();
            }
        }
    }

    [NotNull]
    public string Description
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            using CFString nativeString = new(NativeMethods.CFErrorCopyDescription(handle));
            return nativeString.ToString();
        }
    }

    [MaybeNull]
    public string RecoverySuggestion
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            IntPtr h = NativeMethods.CFErrorCopyRecoverySuggestion(handle);
            if (h == IntPtr.Zero)
            {
                return null;
            }
            else
            {
                using CFString nativeString = new(h);
                return nativeString.ToString();
            }
        }
    }
}