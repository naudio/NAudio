
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    ///     This interface is used to represent the callback object which will 
    ///     receive notification of the completion of an asynchronous operation.
    /// </summary>
    [GeneratedComInterface]
    [Guid("a27003cf-2354-4f2a-8d6a-ab7cff15437e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFAsyncCallback
    {
        /// <summary>
        ///     The GetParameters method allows the asynchronous callback implementer to provide configuration
        ///     information to the callback dispatching mechanism.
        /// </summary>
        /// <param name="pdwFlags">
        ///     Pointer to a 32 bit value containing the flags. Default value is no flags set.
        ///     The following flags are currently defined:
        ///     <para>
        ///         MFASYNC_FAST_IO_PROCESSING_CALLBACK:
        ///             The callback should do very minimal processing (take less than 1 millisecond to complete).
        ///             Longer processing may block device I/O and cause data to be lost or retrieved from or sent to a device late..
        ///     </para>
        ///     <para>
        ///         MFASYNC_SIGNAL_CALLBACK:
        ///             Implies MFASYNC_FAST_IO_PROCESSING_CALLBACK, with the additional restriction that the callback
        ///             does no processing (take much less than 50 microseconds) and the only system call it may make is to SetEvent().
        ///     </para>
        ///     If no flag is set and this callback is not specifically for the long function work queue, the callback should not take a long time to complete (greater than 30ms).
        ///     Longer processing may block other callbacks that need to happen in a timely manner during multimedia processing.
        /// </param>
        /// <param name="pdwQueue">
        ///     Pointer to a double word containing a hint on how to dispatch the callback.  Default value is MFASYNC_CALLBACK_QUEUE_STANDARD.
        ///     The following dispatch hint is currently defined:
        ///         MFASYNC_CALLBACK_QUEUE_STANDARD
        /// </param>
        /// <remarks>
        ///     The callback dispatcher calls this method to determine configuration information for dispatching the callback.
        ///     If this function returns E_NOTIMPL, the callback dispatcher uses default configuration values.
        ///     If the callback wants to mark itself as "realtime safe", it should implement the IRealtime interface, as defined
        ///     in the Media Foundation RealTime Specification.  This will place additional restrictions on what the callback's Invoke method may do.
        /// </remarks>
        [PreserveSig]
        int GetParameters(out uint pdwFlags, out uint pdwQueue); // returns: HRESULT

        /// <summary>
        ///     The Invoke method is called to indicate that the asynchronous operation has completed,
        ///     and to give the callback implementer the result object which can be used to retrieve the output data via a call to EndXXX.
        /// </summary>
        /// <param name="pAsyncResult">
        ///     Pointer to an async result object
        /// </param>
        [PreserveSig]
        int Invoke(IMFAsyncResult pAsyncResult); // returns: HRESULT
    }
}
