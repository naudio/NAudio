using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace NAudio.Wasapi.CoreAudioApi
{
    [GeneratedComClass]
    internal partial class ActivateAudioInterfaceCompletionHandler :
    IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<IAudioClient2> initializeAction;
        private TaskCompletionSource<IAudioClient2> tcs = new TaskCompletionSource<IAudioClient2>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<IAudioClient2> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        public void ActivateCompleted(IntPtr activateOperationPtr)
        {
            // activateOperationPtr is a borrowed callback parameter — we don't own it.
            // GetOrCreateObjectForComInstance (UniqueInstance) takes its own QI'd ref,
            // which we must FinalRelease before returning to keep ref counts balanced.
            var activateOperation = (IActivateAudioInterfaceAsyncOperation)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                activateOperationPtr, CreateObjectFlags.UniqueInstance);
            try
            {
                // First get the activation results, and see if anything bad happened then
                activateOperation.GetActivateResult(out int hr, out var unkPtr);
                if (hr != 0)
                {
                    tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
                    return;
                }

                IAudioClient2 pAudioClient;
                try
                {
                    pAudioClient = (IAudioClient2)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        unkPtr, CreateObjectFlags.UniqueInstance);
                }
                finally
                {
                    Marshal.Release(unkPtr);
                }

                // Next try to call the client's (synchronous, blocking) initialization method.
                try
                {
                    initializeAction(pAudioClient);
                    tcs.SetResult(pAudioClient);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
            finally
            {
                if ((object)activateOperation is ComObject co)
                {
                    co.FinalRelease();
                }
            }
        }

        public TaskAwaiter<IAudioClient2> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }
}
