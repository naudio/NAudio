using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi
{
    static partial class NativeMethods
    {
        /// <summary>
        /// Enables Windows Store apps to access preexisting Component Object Model (COM) interfaces in the WASAPI family.
        /// </summary>
        /// <param name="deviceInterfacePath">A device interface ID for an audio device. This is normally retrieved from a DeviceInformation object or one of the methods of the MediaDevice class.</param>
        /// <param name="riid">The IID of a COM interface in the WASAPI family, such as IAudioClient.</param>
        /// <param name="activationParams">Interface-specific activation parameters. For more information, see the pActivationParams parameter in IMMDevice::Activate. </param>
        /// <param name="completionHandler"></param>
        /// <param name="activationOperation"></param>
        [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void ActivateAudioInterfaceAsync(
            [In, MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [In] IntPtr activationParams, // n.b. is actually a pointer to a PropVariant, but we never need to pass anything but null
            [In] IActivateAudioInterfaceCompletionHandler completionHandler,
            out IActivateAudioInterfaceAsyncOperation activationOperation);

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
