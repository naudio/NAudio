using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.MediaFoundation;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.Wave
{
    /// <summary>
    /// The Media Foundation Resampler Transform
    /// </summary>
    public class MediaFoundationResampler : MediaFoundationTransform
    {
        // CLSID_CResamplerMediaObject — wmcodecdsp.h
        private static readonly Guid ResamplerClsid = new Guid("f447b69e-1884-4a7e-8055-346f74d6edb3");

        private int resamplerQuality;

        private static bool IsPcmOrIeeeFloat(WaveFormat waveFormat)
        {
            var wfe = waveFormat as WaveFormatExtensible;
            return waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                   waveFormat.Encoding == WaveFormatEncoding.IeeeFloat ||
                   (wfe != null && (wfe.SubFormat == AudioSubtypes.MFAudioFormat_PCM
                                    || wfe.SubFormat == AudioSubtypes.MFAudioFormat_Float));
        }

        /// <summary>
        /// Creates the Media Foundation Resampler, allowing modifying of sample rate, bit depth and channel count
        /// </summary>
        /// <param name="sourceProvider">Source provider, must be PCM</param>
        /// <param name="outputFormat">Output format, must also be PCM</param>
        public MediaFoundationResampler(IWaveProvider sourceProvider, WaveFormat outputFormat)
            : base(sourceProvider, outputFormat)
        {
            if (!IsPcmOrIeeeFloat(sourceProvider.WaveFormat))
                throw new ArgumentException("Input must be PCM or IEEE float", "sourceProvider");
            if (!IsPcmOrIeeeFloat(outputFormat))
                throw new ArgumentException("Output must be PCM or IEEE float", "outputFormat");
            MediaFoundationApi.Startup();
            ResamplerQuality = 60; // maximum quality

            // We create the actual transform on demand in the Read method, to avoid
            // apartment-affinity issues. Probe here so a missing resampler DLL fails
            // fast at construction time rather than at first read.
            IntPtr probe = ComActivation.CoCreateInstance(ResamplerClsid, ComActivation.IID_IUnknown);
            Marshal.Release(probe);
        }

        /// <summary>
        /// Creates a resampler with a specified target output sample rate
        /// </summary>
        /// <param name="sourceProvider">Source provider</param>
        /// <param name="outputSampleRate">Output sample rate</param>
        public MediaFoundationResampler(IWaveProvider sourceProvider, int outputSampleRate)
            : this(sourceProvider, CreateOutputFormat(sourceProvider.WaveFormat, outputSampleRate))
        {

        }

        /// <summary>
        /// Creates and configures the actual Resampler transform
        /// </summary>
        /// <returns>A newly created and configured resampler MFT</returns>
        private protected override IMFTransform CreateTransform()
        {
            // Activate via raw CoCreateInstance, then project IMFTransform via the legacy
            // COM marshaller (MediaFoundationTransform.transform is still typed as the
            // legacy [ComImport] IMFTransform) and IWMResamplerProps via the modern
            // ComWrappers path. Both views share the same underlying COM object.
            IntPtr unknown = ComActivation.CoCreateInstance(ResamplerClsid, ComActivation.IID_IUnknown);
            try
            {
                var resamplerTransform = (IMFTransform)Marshal.GetObjectForIUnknown(unknown);

                using (var inputMediaFormat = new MediaType(sourceProvider.WaveFormat))
                {
                    resamplerTransform.SetInputType(0, inputMediaFormat.MediaFoundationObject, 0);
                }

                using (var outputMediaFormat = new MediaType(outputWaveFormat))
                {
                    resamplerTransform.SetOutputType(0, outputMediaFormat.MediaFoundationObject, 0);
                }

                var props = ComActivation.WrapUnique<NAudio.Dmo.Interfaces.IWMResamplerProps>(unknown);
                try
                {
                    // 60 is the best quality, 1 is linear interpolation
                    props.SetHalfFilterLength(ResamplerQuality);
                }
                finally
                {
                    ((ComObject)(object)props).FinalRelease();
                }

                return resamplerTransform;
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        /// <summary>
        /// Gets or sets the Resampler quality. n.b. set the quality before starting to resample.
        /// 1 is lowest quality (linear interpolation) and 60 is best quality
        /// </summary>
        public int ResamplerQuality
        {
            get { return resamplerQuality; }
            set 
            { 
                if (value < 1 || value > 60)
                    throw new ArgumentOutOfRangeException(nameof(value), "Resampler Quality must be between 1 and 60");
                resamplerQuality = value; 
            }
        }

        private static WaveFormat CreateOutputFormat(WaveFormat inputFormat, int outputSampleRate)
        {
            WaveFormat outputFormat;
            if (inputFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                outputFormat = new WaveFormat(outputSampleRate,
                    inputFormat.BitsPerSample,
                    inputFormat.Channels);
            }
            else if (inputFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputSampleRate,
                    inputFormat.Channels);
            }
            else
            {
                throw new ArgumentException("Can only resample PCM or IEEE float");
            }
            return outputFormat;
        }



    }
}
