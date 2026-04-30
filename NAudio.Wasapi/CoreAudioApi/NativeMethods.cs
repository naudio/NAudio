using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi
{
    static partial class NativeMethods
    {
        public const int CLSCTX_INPROC_SERVER = 0x1;

        /// <summary>
        /// Creates a single uninitialized object of the class associated with a specified CLSID.
        /// Used to activate COM objects directly (without going through the legacy
        /// <c>[ComImport]</c> coclass / RCW path) so that the resulting interface pointer
        /// can be projected via <see cref="System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers"/>.
        /// </summary>
        [LibraryImport("ole32.dll")]
        public static partial int CoCreateInstance(
            in Guid rclsid,
            IntPtr pUnkOuter,
            int dwClsContext,
            in Guid riid,
            out IntPtr ppv);

        /// <summary>
        /// Enables Windows Store apps to access preexisting Component Object Model (COM) interfaces in the WASAPI family.
        /// </summary>
        /// <param name="deviceInterfacePath">A device interface ID for an audio device. This is normally retrieved from a DeviceInformation object or one of the methods of the MediaDevice class.</param>
        /// <param name="riid">The IID of a COM interface in the WASAPI family, such as IAudioClient.</param>
        /// <param name="activationParams">Interface-specific activation parameters. For more information, see the pActivationParams parameter in IMMDevice::Activate. </param>
        /// <param name="completionHandler">Raw COM pointer to an IActivateAudioInterfaceCompletionHandler implementation. Caller must obtain it via ComWrappers.GetOrCreateComInterfaceForObject and QI for IID_IActivateAudioInterfaceCompletionHandler before passing — multi-vtable CCWs return distinct pointers per interface.</param>
        /// <param name="activationOperation">Raw COM pointer to the resulting IActivateAudioInterfaceAsyncOperation. Caller must release.</param>
        [LibraryImport("Mmdevapi.dll", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int ActivateAudioInterfaceAsync(
            string deviceInterfacePath,
            in Guid riid,
            IntPtr activationParams, // n.b. is actually a pointer to a PropVariant, but we never need to pass anything but null
            IntPtr completionHandler,
            out IntPtr activationOperation);

        /// <summary>
        /// Associates the calling thread with the specified MMCSS task. This elevates the
        /// thread priority to reduce audio glitching under CPU load.
        /// </summary>
        /// <param name="taskName">Task name, e.g. "Pro Audio", "Audio", "Playback", "Capture"</param>
        /// <param name="taskIndex">Task index. Pass 0 to create a new task.</param>
        /// <returns>A handle used to revert the thread characteristics, or IntPtr.Zero on failure.</returns>
        [DllImport("avrt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr AvSetMmThreadCharacteristics(string taskName, ref uint taskIndex);

        /// <summary>
        /// Reverts the thread characteristics set by AvSetMmThreadCharacteristics.
        /// </summary>
        /// <param name="avrtHandle">The handle returned by AvSetMmThreadCharacteristics.</param>
        /// <returns>True if successful.</returns>
        [DllImport("avrt.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AvRevertMmThreadCharacteristics(IntPtr avrtHandle);
    }
}
