
using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// Provides P/Invokes for some of the Core Foundation framework
/// API's, which are needed for interacting with the rest API's 
/// that are complementing the macOS support.
/// </summary>
internal static partial class NativeMethods
{
    #region Basic Core Foundation object management

    // CF_EXPORT void CFRelease(CFTypeRef cf);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial void CFRelease(IntPtr cfObject);

    // CF_EXPORT CFIndex CFGetRetainCount(CFTypeRef cf);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial CFIndex CFGetRetainCount(IntPtr cfObject);

    // CF_EXPORT Boolean CFEqual(CFTypeRef cf1, CFTypeRef cf2);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial MacBoolean CFEqual(IntPtr cfObject1, IntPtr cfObject2);

    // CF_EXPORT CFHashCode CFHash(CFTypeRef cf);
    // CFHashCode is a unsigned long in 32 bit or a unsigned long long value on 64-bit.
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial CFHashCode CFHash(IntPtr cfObject);

    // CF_EXPORT CFStringRef CFCopyDescription(CFTypeRef cf);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFCopyDescription(IntPtr cfObject);

    // CF_EXPORT CFAllocatorRef CFGetAllocator(CFTypeRef cf);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFGetAllocator(IntPtr cfObject);

    // CF_EXPORT CFTypeRef CFRetain(CFTypeRef cf);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFRetain(IntPtr cfObject);

    #endregion

    #region CFAllocator native methods

    // CF_EXPORT CFAllocatorRef CFAllocatorGetDefault(void);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFAllocatorGetDefault();

    // CF_EXPORT void *CFAllocatorAllocate(CFAllocatorRef allocator, CFIndex size, CFOptionFlags hint) _CF_TYPED_ALLOC(CFAllocatorAllocateTyped, 2);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFAllocatorAllocate(IntPtr allocator, CFIndex size, CULong hint = default);

    // CF_EXPORT void *CFAllocatorReallocate(CFAllocatorRef allocator, void *ptr, CFIndex newsize, CFOptionFlags hint) _CF_TYPED_ALLOC(CFAllocatorReallocateTyped, 3);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFAllocatorReallocate(IntPtr allocator, IntPtr ptr, CFIndex newsize, CULong hint = default);

    // CF_EXPORT void CFAllocatorDeallocate(CFAllocatorRef allocator, void *ptr);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial void CFAllocatorDeallocate(IntPtr allocator, IntPtr ptr);

    // CF_EXPORT CFIndex CFAllocatorGetPreferredSizeForSize(CFAllocatorRef allocator, CFIndex size, CFOptionFlags hint);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial CFIndex CFAllocatorGetPreferredSizeForSize(IntPtr allocator, CFIndex size, CULong hint = default);

    #endregion

    #region CFString native methods

    // CF_EXPORT CFStringRef CFStringCreateWithCharacters(CFAllocatorRef alloc, const UniChar *chars, CFIndex numChars);
    [LibraryImport(MacLibraries.CoreFoundation, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr CFStringCreateWithCharacters(IntPtr allocator, string characters, CFIndex numChars);

    // CF_EXPORT CFStringRef CFStringCreateCopy(CFAllocatorRef alloc, CFStringRef theString) CF_FORMAT_ARGUMENT(2);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFStringCreateCopy(IntPtr allocator, IntPtr theString);

    // CF_EXPORT CFIndex CFStringGetLength(CFStringRef theString);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial CFIndex CFStringGetLength(IntPtr theString);

    // CF_EXPORT UniChar CFStringGetCharacterAtIndex(CFStringRef theString, CFIndex idx);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial ushort CFStringGetCharacterAtIndex(IntPtr theString, CFIndex idx);

    // CF_EXPORT const UniChar *CFStringGetCharactersPtr(CFStringRef theString);  
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static unsafe partial char* CFStringGetCharactersPtr(IntPtr theString); /* May return NULL at any time; be prepared for NULL, if not now, in some other time or place. */

    // CF_EXPORT void CFStringGetCharacters(CFStringRef theString, CFRange range, UniChar *buffer);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static unsafe partial void CFStringGetCharacters(IntPtr theString, CFRange range, char* buffer);

    #endregion

    #region CFURL native methods

    // CF_EXPORT CFURLRef CFURLCreateWithString(CFAllocatorRef allocator, CFStringRef URLString, CFURLRef baseURL);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFURLCreateWithString(IntPtr allocator, IntPtr URLString, IntPtr baseURL);

    // CF_EXPORT CFStringRef CFURLGetString(CFURLRef anURL);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFURLGetString(IntPtr anURL);

    // CF_EXPORT CFURLRef CFURLGetBaseURL(CFURLRef anURL);
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFURLGetBaseURL(IntPtr anURL);

    #endregion

    #region CFError native methods

    // CF_EXPORT CFErrorRef CFErrorCreate(CFAllocatorRef allocator, CFErrorDomain domain, CFIndex code, CFDictionaryRef userInfo)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFErrorCreate(IntPtr allocator, IntPtr domain, CFIndex code, IntPtr userInfo = default);

    // CF_EXPORT CFErrorDomain CFErrorGetDomain(CFErrorRef err)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFErrorGetDomain(IntPtr err);

    // CF_EXPORT CFIndex CFErrorGetCode(CFErrorRef err)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial CFIndex CFErrorGetCode(IntPtr err);

    // CF_EXPORT CFStringRef CFErrorCopyDescription(CFErrorRef err)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFErrorCopyDescription(IntPtr err);

    // CF_EXPORT CFStringRef CFErrorCopyFailureReason(CFErrorRef err)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFErrorCopyFailureReason(IntPtr err);

    // CF_EXPORT CFStringRef CFErrorCopyRecoverySuggestion(CFErrorRef err)
    [LibraryImport(MacLibraries.CoreFoundation)]
    public static partial IntPtr CFErrorCopyRecoverySuggestion(IntPtr err);

    #endregion

}