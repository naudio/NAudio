
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreFoundationApi;

/// <summary>
/// Provides the base exception class related with errors of
/// the CoreFoundation library. <br />
/// This is the only type exposed to the consumers of the library,
/// and cannot be instantiated outside of the assembly.
/// </summary>
public class CoreFoundationException : MacException
{
    internal CoreFoundationException(int osStatus) : base(osStatus) { }

    internal CoreFoundationException([AllowNull] string message, int osStatus) : base(message, osStatus) { }
}