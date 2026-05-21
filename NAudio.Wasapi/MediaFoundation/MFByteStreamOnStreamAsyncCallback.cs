
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

namespace NAudio.MediaFoundation
{
    [GeneratedComClass]
    internal sealed partial class MfByteStreamOnStreamAsyncCallback : IMFAsyncCallback
    {
        private readonly int size;
        private readonly nint pointer;
        private readonly Callback callbackToCall;

        // Read/Write method reference to be called when the below Invoke implementation gets called.
        public delegate int Callback(nint pointer, int size, out int sizeOut);

        public MfByteStreamOnStreamAsyncCallback(Callback callback, nint pointer, int size)
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

        public int Invoke(IntPtr resultObjectPtr)
        {
            IMFAsyncResult currentResult = (IMFAsyncResult)ComActivation
                .ComWrappers
                .GetOrCreateObjectForComInstance(resultObjectPtr, CreateObjectFlags.UniqueInstance);
            try
            {
                int hr = callbackToCall.Invoke(pointer, size, out int dataSize);
                currentResult.SetStatus(hr);
                if (HResult.IsError(hr))
                {
                    hr = HResult.E_UNEXPECTED;
                }
                else
                {
                    hr = currentResult.GetObject(out nint innerResultPointer);
                    if (!HResult.IsError(hr))
                    {
                        IMFAsyncResult innerResultWrapper = null;
                        IMfByteStreamAsyncCallSizeHandler sizeHandler = null;
                        try
                        {
                            innerResultWrapper = (IMFAsyncResult)ComActivation
                                .ComWrappers
                                .GetOrCreateObjectForComInstance(innerResultPointer, CreateObjectFlags.UniqueInstance);

                            hr = innerResultWrapper.GetObject(out nint sizeHandlerPtr);

                            if (!HResult.IsError(hr))
                            {
                                try
                                {
                                    sizeHandler = (IMfByteStreamAsyncCallSizeHandler)
                                        ComActivation
                                        .ComWrappers
                                        .GetOrCreateObjectForComInstance(sizeHandlerPtr, CreateObjectFlags.UniqueInstance);
                                    sizeHandler.SetDataSize(dataSize);
                                }
                                finally
                                {
                                    ComActivation.ReleaseBoth(sizeHandler, sizeHandlerPtr);
                                }

                                hr = MediaFoundationApi.InvokeCallback(innerResultPointer);
                            }
                        }
                        finally
                        {
                            // https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nf-mfobjects-imfasyncresult-getobject#parameters says:
                            // "If the value is not NULL, the caller must release the interface."
                            ComActivation.ReleaseBoth(innerResultWrapper, innerResultPointer);
                        }
                    }
                }

                return hr;
            }
            catch (COMException cex)
            {
                return cex.GetHResult();
            }
            finally
            {
                ComActivation.ReleaseBoth(currentResult, IntPtr.Zero);
            }
        }
    }
}
