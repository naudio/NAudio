using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Wrapper for <c>IAudioMeterInformation</c>. Reports the master peak value and
    /// per-channel peak values for an audio stream or endpoint.
    /// </summary>
    public class AudioMeterInformation : IDisposable
    {
        private IAudioMeterInformation audioMeterInformation;
        private readonly bool ownsInterface;

        /// <summary>
        /// Wraps a freshly activated <c>IAudioMeterInformation</c>. The supplied COM
        /// pointer is consumed: this instance owns the lifetime and releases it on
        /// <see cref="Dispose"/>.
        /// </summary>
        internal AudioMeterInformation(IntPtr nativePointer)
        {
            try
            {
                audioMeterInformation = (IAudioMeterInformation)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    nativePointer, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
            ownsInterface = true;

            CoreAudioException.ThrowIfFailed(audioMeterInformation.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            PeakValues = new AudioMeterInformationChannels(audioMeterInformation);
        }

        /// <summary>
        /// Wraps an <c>IAudioMeterInformation</c> obtained via QI on an existing RCW
        /// (typically from an <c>AudioSessionControl</c>). The parent owns the
        /// lifetime; this instance does not release on disposal.
        /// </summary>
        internal AudioMeterInformation(IAudioMeterInformation borrowed)
        {
            audioMeterInformation = borrowed;
            ownsInterface = false;

            CoreAudioException.ThrowIfFailed(audioMeterInformation.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            PeakValues = new AudioMeterInformationChannels(audioMeterInformation);
        }

        /// <summary>
        /// Per-channel peak values for the metered stream.
        /// </summary>
        public AudioMeterInformationChannels PeakValues { get; }

        /// <summary>
        /// Bitmask describing which metering features the underlying hardware supports
        /// (peak meter, RMS meter, hardware-accelerated metering).
        /// </summary>
        public EEndpointHardwareSupport HardwareSupport { get; }

        /// <summary>
        /// Master peak value across all channels of the metered stream, in the
        /// normalized range <c>0.0</c> to <c>1.0</c>.
        /// </summary>
        public float MasterPeakValue
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioMeterInformation.GetPeakValue(out var result));
                return result;
            }
        }

        /// <summary>
        /// Releases the underlying COM reference when this wrapper owns it.
        /// Borrowed wrappers (constructed from a parent's RCW) do not release.
        /// </summary>
        public void Dispose()
        {
            if (audioMeterInformation != null)
            {
                if (ownsInterface && (object)audioMeterInformation is ComObject co)
                {
                    co.FinalRelease();
                }
                audioMeterInformation = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
