using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi
{
    static class NativeMethods
    {

        /// <summary>
        /// Enables Windows Store apps to access preexisting Component Object Model (COM) interfaces in the WASAPI family.
        /// https://docs.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nf-mmdeviceapi-activateaudiointerfaceasync
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
    }
}
