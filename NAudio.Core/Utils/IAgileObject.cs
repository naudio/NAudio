
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Utils
{
    /// <summary>
    /// Interface implemented on some Windows 8+ COM objects, notably some WinRT ones
    /// </summary>
    [GeneratedComInterface]
    [Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90")]
    [SupportedOSPlatform(WindowsVersions.NTDDI_WIN8)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IAgileObject { }
}
