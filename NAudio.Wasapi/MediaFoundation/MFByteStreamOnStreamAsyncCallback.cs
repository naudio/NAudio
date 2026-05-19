
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.MediaFoundation.Interfaces;

namespace NAudio.MediaFoundation
{
    [GeneratedComClass]
    internal sealed partial class MFByteStreamOnStreamAsyncCallback : IMFAsyncCallback
    {
        private readonly int size;
        private readonly IntPtr pointer;
        private readonly bool read_mode;
        private readonly MFByteStreamFromStream wrapper;

        public MFByteStreamOnStreamAsyncCallback(
            MFByteStreamFromStream wrapper,
            IntPtr pointer,
            int size,
            bool read_mode
        )
        {
            this.size = size;
            this.pointer = pointer;
            this.wrapper = wrapper;
            this.read_mode = read_mode;
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
            int hr, ds;

            try
            {
                if (read_mode)
                {
                    hr = wrapper.Read(pointer, size, out ds);
                }
                else
                {
                    hr = wrapper.Write(pointer, size, out ds);
                }
                result.SetStatus(hr);
                if (HResult.IsError(hr))
                {
                    hr = HResult.E_UNEXPECTED;
                }
                else
                {
                    hr = result.GetObject(out IntPtr result_pointer);
                    if (HResult.IsError(hr))
                    {
                        hr = HResult.E_UNEXPECTED;
                    }
                    else
                    {
                        wrapper.SetAsyncCallSize(result_pointer, ds);
                        hr = MediaFoundationApi.InvokeCallback(result_pointer);
                        // https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nf-mfobjects-imfasyncresult-getobject#parameters says:
                        // "If the value is not NULL, the caller must release the interface."
                        Marshal.Release(result_pointer);
                    }
                }
            }
            catch (COMException cex)
            {
                result.SetStatus(cex.GetHResult());
                hr = HResult.E_UNEXPECTED;
            }

            return hr;
        }
    }
}
