using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// is defined in propsys.h
    /// </summary>
    /// <remarks>
    /// GetValue and SetValue take an unmanaged PROPVARIANT buffer (IntPtr) rather than a
    /// marshalled struct. The source-generated COM marshaller cannot blit PropVariant's
    /// [StructLayout(LayoutKind.Explicit)] union, so callers allocate the buffer themselves
    /// and use Unsafe.Read/Write plus PropVariantClear for ownership.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out int propCount);

        [PreserveSig]
        int GetAt(int property, out PropertyKey key);

        [PreserveSig]
        int GetValue(in PropertyKey key, IntPtr propVariantBuffer);

        [PreserveSig]
        int SetValue(in PropertyKey key, IntPtr propVariantBuffer);

        [PreserveSig]
        int Commit();
    }
}
