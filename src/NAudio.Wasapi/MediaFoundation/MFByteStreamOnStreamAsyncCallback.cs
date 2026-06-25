
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

namespace NAudio.MediaFoundation;

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
        // Let's be sure that our callback will be invoked on the multi-threaded queue.
        pdwFlags = 0;
        pdwQueue = MediaFoundationInterop.MFASYNC_CALLBACK_QUEUE_MULTITHREADED;
        return HResult.S_OK;
    }

    public int Invoke(IntPtr resultObjectPtr)
    {
        IMFAsyncResult currentResult = (IMFAsyncResult)ComActivation
            .ComWrappers
            .GetOrCreateObjectForComInstance(resultObjectPtr, CreateObjectFlags.UniqueInstance);
        try
        {
            int callbackHResult = callbackToCall.Invoke(pointer, size, out int dataSize);

            currentResult.SetStatus(callbackHResult);

            int result = currentResult.GetObject(out nint innerResultPointer);
            if (!HResult.IsError(result))
            {
                IMFAsyncResult innerResultWrapper = null;
                IMfByteStreamAsyncCallSizeHandler sizeHandler = null;
                try
                {
                    innerResultWrapper = (IMFAsyncResult)ComActivation
                        .ComWrappers
                        .GetOrCreateObjectForComInstance(innerResultPointer, CreateObjectFlags.UniqueInstance);

                    innerResultWrapper.SetStatus(callbackHResult);

                    result = innerResultWrapper.GetObject(out nint sizeHandlerPtr);

                    if (!HResult.IsError(result))
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

                        return MediaFoundationApi.InvokeCallback(innerResultPointer);
                    }
                }
                finally
                {
                    // https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nf-mfobjects-imfasyncresult-getobject#parameters says:
                    // "If the value is not NULL, the caller must release the interface."
                    ComActivation.ReleaseBoth(innerResultWrapper, innerResultPointer);
                }
            }

            return result;
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
