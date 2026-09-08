// This interop definition was derived from the file CFURL.h of the Core Foundation Framework.
// See https://developer.apple.com/documentation/corefoundation for more information.

namespace NAudio.MacOS.CoreFoundationApi;

internal enum CFURLPathStyle : long
{
    kCFURLPOSIXPathStyle = 0,
    // kCFURLHFSPathStyle API_DEPRECATED("Carbon File Manager is deprecated, use kCFURLPOSIXPathStyle where possible", macos(10.0,10.9), ios(2.0,7.0), watchos(2.0,2.0), tvos(9.0,9.0)), /* The use of kCFURLHFSPathStyle is deprecated. The Carbon File Manager, which uses HFS style paths, is deprecated. HFS style paths are unreliable because they can arbitrarily refer to multiple volumes if those volumes have identical volume names. You should instead use kCFURLPOSIXPathStyle wherever possible. */
    kCFURLWindowsPathStyle = 2
}