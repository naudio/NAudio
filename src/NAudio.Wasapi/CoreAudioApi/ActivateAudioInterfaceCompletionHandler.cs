using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace NAudio.CoreAudioApi;

[GeneratedComClass]
internal partial class ActivateAudioInterfaceCompletionHandler :
IActivateAudioInterfaceCompletionHandler, IAgileObject
{
    private readonly Action<IAudioClient> initializeAction;
    private readonly TaskCompletionSource<IAudioClient> tcs = new();

    public ActivateAudioInterfaceCompletionHandler(
        Action<IAudioClient> initializeAction)
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

            // Wrap as the base IAudioClient: the process-loopback virtual device returns a client
            // that does NOT support IAudioClient2, so casting to IAudioClient2 here would throw.
            // Callers that need IAudioClient2 features QI for it from the returned client.
            IAudioClient pAudioClient;
            try
            {
                pAudioClient = (IAudioClient)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
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

    public TaskAwaiter<IAudioClient> GetAwaiter()
    {
        return tcs.Task.GetAwaiter();
    }

    /// <summary>
    /// The underlying activation task. Await this with <c>ConfigureAwait(false)</c> when the
    /// caller may be on a thread with a synchronization context (e.g. WPF/WinForms) to avoid
    /// marshalling the continuation back onto a thread that might be blocked.
    /// </summary>
    public Task<IAudioClient> Completion => tcs.Task;
}
