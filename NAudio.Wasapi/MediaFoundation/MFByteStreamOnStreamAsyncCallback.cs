
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

namespace NAudio.MediaFoundation
{
    [GeneratedComClass]
    internal sealed partial class MFByteStreamOnStreamAsyncCallback : IMFAsyncCallback
    {
        private readonly int size;
        private readonly nint pointer;
        private readonly Callback callbackToCall;

        // Read/Write method reference to be called when the below Invoke implementation gets called.
        public delegate int Callback(nint pointer, int size, out int sizeOut);

        public MFByteStreamOnStreamAsyncCallback(Callback callback, nint pointer, int size)
        {
            this.size = size;
            this.pointer = pointer;
            this.callbackToCall = callback;
        }

        public int GetParameters(out uint pdwFlags, out uint pdwQueue)
        {
            pdwFlags = 0;
            pdwQueue = MediaFoundationApi.AllocatedWorkQueueToken;
            return HResult.S_OK;
        }

        // Note: Here using the marshalled result works as expected,
        // because we are into an RCW which is not even managed by us.
        public int Invoke(IMFAsyncResult result)
        {
            try
            {
                int hr = callbackToCall.Invoke(pointer, size, out int dataSize);
                result.SetStatus(hr);
                if (HResult.IsError(hr))
                {
                    hr = HResult.E_UNEXPECTED;
                }
                else
                {
                    hr = result.GetObject(out nint resultPointer);
                    if (!HResult.IsError(hr))
                    {
                        IMFAsyncResult innerResult = null;
                        IMfByteStreamAsyncCallSizeHandler handler = null;
                        try
                        {
                            innerResult = (IMFAsyncResult)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(resultPointer, CreateObjectFlags.UniqueInstance);

                            hr = innerResult.GetObject(out nint sizeObject);

                            if (!HResult.IsError(hr))
                            {
                                try
                                {
                                    handler = (IMfByteStreamAsyncCallSizeHandler)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(sizeObject, CreateObjectFlags.UniqueInstance);
                                    handler.SetDataSize(dataSize);
                                }
                                finally
                                {
                                    ComActivation.ReleaseBoth(handler, sizeObject);
                                }

                                hr = MediaFoundationApi.InvokeCallback(resultPointer);
                            }
                        }
                        finally
                        {
                            // https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nf-mfobjects-imfasyncresult-getobject#parameters says:
                            // "If the value is not NULL, the caller must release the interface."
                            ComActivation.ReleaseBoth(innerResult, resultPointer);
                        }
                    }
                }

                return hr;
            }
            catch (COMException cex)
            {
                return cex.GetHResult();
            }
        }
    }
}
