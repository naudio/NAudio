using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interfaces
{
    /// <summary>
    /// Enumerates DirectX Media Objects (DMOs) registered in the system.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IEnumDMO (dmoreg.h).
    /// https://learn.microsoft.com/windows/win32/api/dmoreg/nn-dmoreg-ienumdmo
    /// </remarks>
    [GeneratedComInterface]
    [Guid("2c3cd98a-2bfa-4a53-9c27-5249ba64ba0f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IEnumDmo
    {
        [PreserveSig]
        int Next(int itemsToFetch, out Guid clsid, out IntPtr name, out int itemsFetched);

        [PreserveSig]
        int Skip(int itemsToSkip);

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int Clone(out IntPtr enumPointer);
    }
}
