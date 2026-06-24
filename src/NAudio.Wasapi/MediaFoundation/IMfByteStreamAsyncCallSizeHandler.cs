
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    // This is not an actual interface in Windows COM, it is defined by ourselves to
    // provide the size data back to the async call in the IMFByteStream handler.
    // Note - the GUID used below is randomly generated via System.Guid.NewGuid,
    // it is just defined so that the COM interop generator does not complain about
    // the missing GUID on the interface.
    [GeneratedComInterface]
    [Guid("af1b8de9-d41e-4e22-b17b-5a3cd4ed75f7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMfByteStreamAsyncCallSizeHandler
    {
        [PreserveSig]
        int GetDataSize();

        [PreserveSig]
        void SetDataSize(int dataSize);
    }

    // Our only implementation of the above interface.
    [GeneratedComClass]
    internal sealed partial class MfByteStreamAsyncCallSizeHandler : IMfByteStreamAsyncCallSizeHandler
    {
        private int dataSize;

        public int GetDataSize() => dataSize;

        public void SetDataSize(int dataSize) => this.dataSize = dataSize;
    }
}
