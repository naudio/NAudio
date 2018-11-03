using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxWavesReverb
    {
        public float InGain;
        public float ReverbMix;
        public float ReverbTime;
        public float HighFreqRtRatio;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("46858c3a-0dc6-45e3-b760-d4eef16cb325"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXWavesReverb
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxWavesReverb param);

        [PreserveSig]
        int GetAllParameters(out DsFxWavesReverb param);
    }

    /// <summary>
    /// DMO Reverb Effect
    /// </summary>
    public class DmoWavesReverb : IDmoEffector<DmoWavesReverb.Params>
    {
        /// <summary>
        /// DMO Reverb Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFX_WAVESREVERB_INGAIN_MIN
            /// </summary>
            public const float InGainMin = -96.0f;
            /// <summary>
            /// DSFX_WAVESREVERB_INGAIN_MAX
            /// </summary>
            public const float InGainMax = 0.0f;
            /// <summary>
            /// DSFX_WAVESREVERB_INGAIN_DEFAULT
            /// </summary>
            public const float InGainDefault = 0.0f;

            /// <summary>
            /// DSFX_WAVESREVERB_REVERBMIX_MIN
            /// </summary>
            public const float ReverbMixMin = -96.0f;
            /// <summary>
            /// DSFX_WAVESREVERB_REVERBMIX_MAX
            /// </summary>
            public const float ReverbMixMax = 0.0f;
            /// <summary>
            /// DSFX_WAVESREVERB_REVERBMIX_DEFAULT
            /// </summary>
            public const float ReverbMixDefault = 0.0f;

            /// <summary>
            /// DSFX_WAVESREVERB_REVERBTIME_MIN
            /// </summary>
            public const float ReverbTimeMin = 0.001f;
            /// <summary>
            /// DSFX_WAVESREVERB_REVERBTIME_MAX
            /// </summary>
            public const float ReverbTimeMax = 3000.0f;
            /// <summary>
            /// DSFX_WAVESREVERB_REVERBTIME_DEFAULT
            /// </summary>
            public const float ReverbTimeDefault = 1000.0f;

            /// <summary>
            /// DSFX_WAVESREVERB_HIGHFREQRTRATIO_MIN
            /// </summary>
            public const float HighFreqRtRatioMin = 0.001f;
            /// <summary>
            /// DSFX_WAVESREVERB_HIGHFREQRTRATIO_MAX
            /// </summary>
            public const float HighFreqRtRatioMax = 0.999f;
            /// <summary>
            /// DSFX_WAVESREVERB_HIGHFREQRTRATIO_DEFAULT
            /// </summary>
            public const float HighFreqRtRatioDefault = 0.001f;

            /// <summary>
            /// Input gain of signal, in decibels (dB).
            /// </summary>
            public float InGain
            {
                get
                {
                    var param = GetAllParameters();
                    return param.InGain;
                }
                set
                {
                    var param = GetAllParameters();
                    param.InGain = Math.Max(Math.Min(InGainMax, value), InGainMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Reverb mix, in dB.
            /// </summary>
            public float ReverbMix
            {
                get
                {
                    var param = GetAllParameters();
                    return param.ReverbMix;
                }
                set
                {
                    var param = GetAllParameters();
                    param.ReverbMix = Math.Max(Math.Min(ReverbMixMax, value), ReverbMixMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Reverb time, in milliseconds.
            /// </summary>
            public float ReverbTime
            {
                get
                {
                    var param = GetAllParameters();
                    return param.ReverbTime;
                }
                set
                {
                    var param = GetAllParameters();
                    param.ReverbTime = Math.Max(Math.Min(ReverbTimeMax, value), ReverbTimeMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// High-frequency reverb time ratio.
            /// </summary>
            public float HighFreqRtRatio
            {
                get
                {
                    var param = GetAllParameters();
                    return param.HighFreqRtRatio;
                }
                set
                {
                    var param = GetAllParameters();
                    param.HighFreqRtRatio = Math.Max(Math.Min(HighFreqRtRatioMax, value), HighFreqRtRatioMin);
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXWavesReverb fxWavesReverb;

            internal Params(IDirectSoundFXWavesReverb dsFxObject)
            {
                fxWavesReverb = dsFxObject;
            }

            private void SetAllParameters(DsFxWavesReverb param)
            {
                Marshal.ThrowExceptionForHR(fxWavesReverb.SetAllParameters(ref param));
            }

            private DsFxWavesReverb GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxWavesReverb.GetAllParameters(out var param));
                return param;
            }
        }

        private readonly MediaObject mediaObject;
        private readonly MediaObjectInPlace mediaObjectInPlace;
        private readonly Params effectParams;

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject => mediaObject;

        /// <summary>
        /// Media Object InPlace
        /// </summary>
        public MediaObjectInPlace MediaObjectInPlace => mediaObjectInPlace;

        /// <summary>
        /// Effect Parameter
        /// </summary>
        public Params EffectParams => effectParams;

        /// <summary>
        /// Create new DMO WavesReverb
        /// </summary>
        public DmoWavesReverb()
        {
            var guidWavesReverb = new Guid("87FC0268-9A55-4360-95AA-004A1D9DE26C");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidWavesReverb));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject) mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace) mediaComObject);
                effectParams = new Params((IDirectSoundFXWavesReverb) mediaComObject);
            }
        }

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose()
        {
            mediaObjectInPlace?.Dispose();
            mediaObject?.Dispose();
        }
    }
}