using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    [GeneratedComInterface]
    [Guid("5BC8A76B-869A-46A3-9B03-FA218A66AEBE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFCollection
    {
        [PreserveSig]
        int GetElementCount(out int pcElements);

        [PreserveSig]
        int GetElement(int dwElementIndex, out IntPtr ppUnkElement);

        [PreserveSig]
        int AddElement(IntPtr pUnkElement);

        [PreserveSig]
        int RemoveElement(int dwElementIndex, out IntPtr ppUnkElement);

        [PreserveSig]
        int InsertElementAt(int dwIndex, IntPtr pUnknown);

        [PreserveSig]
        int RemoveAllElements();
    }
}
