using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxFlanger
    {
        public float WetDryMix;
        public float Depth;
        public float FeedBack;
        public float Frequency;
        public FlangerWaveForm WaveForm;
        public float Delay;
        public FlangerPhase Phase;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("903e9878-2c92-4072-9b2c-ea68f5396783"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXFlanger
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxFlanger param);

        [PreserveSig]
        int GetAllParameters(out DsFxFlanger param);
    }

    /// <summary>
    /// DMO Flanger Effect
    /// </summary>
    public class DmoFlanger : IDmoEffector<DmoFlanger.Params>
    {
        /// <summary>
        /// DMO Flanger Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXFLANGER_WETDRYMIX_MIN
            /// </summary>
            public const float WetDryMixMin = 0.0f;
            /// <summary>
            /// DSFXFLANGER_WETDRYMIX_MAX
            /// </summary>
            public const float WetDryMixMax = 100.0f;
            /// <summary>
            /// DSFXFLANGER_WETDRYMIX_DEFAULT
            /// </summary>
            public const float WetDrtMixDefault = 50.0f;

            /// <summary>
            /// DSFXFLANGER_DEPTH_MIN
            /// </summary>
            public const float DepthMin = 0.0f;
            /// <summary>
            /// DSFXFLANGER_DEPTH_MAX
            /// </summary>
            public const float DepthMax = 100.0f;
            /// <summary>
            /// DSFXFLANGER_DEPTH_DEFAULT
            /// </summary>
            public const float DepthDefault = 100.0f;

            /// <summary>
            /// DSFXFLANGER_FEEDBACK_MIN
            /// </summary>
            public const float FeedBackMin = -99.0f;
            /// <summary>
            /// DSFXFLANGER_FEEDBACK_MAX
            /// </summary>
            public const float FeedBackMax = 99.0f;
            /// <summary>
            /// DSFXFLANGER_FEEDBACK_DEFAULT
            /// </summary>
            public const float FeedBaclDefault = -50.0f;

            /// <summary>
            /// DSFXFLANGER_FREQUENCY_MIN
            /// </summary>
            public const float FrequencyMin = 0.0f;
            /// <summary>
            /// DSFXFLANGER_FREQUENCY_MAX
            /// </summary>
            public const float FrequencyMax = 10.0f;
            /// <summary>
            /// DSFXFLANGER_FREQUENCY_DEFAULT
            /// </summary>
            public const float FrequencyDefault = 0.25f;

            /// <summary>
            /// DSFXFLANGER_WAVE_DEFAULT
            /// </summary>
            public const FlangerWaveForm WaveFormDefault = FlangerWaveForm.Sin;

            /// <summary>
            /// DSFXFLANGER_DELAY_MIN
            /// </summary>
            public const float DelayMin = 0.0f;
            /// <summary>
            /// DSFXFLANGER_DELAY_MAX
            /// </summary>
            public const float DelayMax = 4.0f;
            /// <summary>
            /// DSFXFLANGER_DELAY_DEFAULT
            /// </summary>
            public const float DelayDefault = 2.0f;

            /// <summary>
            /// DSFXFLANGER_PHASE_DEFAULT
            /// </summary>
            public const FlangerPhase PhaseDefault = FlangerPhase.Zero;

            /// <summary>
            /// Ratio of wet (processed) signal to dry (unprocessed) signal.
            /// </summary>
            public float WetDryMix
            {
                get
                {
                    var param = GetAllParameters();
                    return param.WetDryMix;
                }
                set
                {
                    var param = GetAllParameters();
                    param.WetDryMix = Math.Max(Math.Min(WetDryMixMax, value), WetDryMixMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Percentage by which the delay time is modulated by the low-frequency oscillator,
            /// in hundredths of a percentage point.
            /// </summary>
            public float Depth
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Depth;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Depth = Math.Max(Math.Min(DepthMax, value), DepthMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Percentage of output signal to feed back into the effect's input.
            /// </summary>
            public float FeedBack
            {
                get
                {
                    var param = GetAllParameters();
                    return param.FeedBack;
                }
                set
                {
                    var param = GetAllParameters();
                    param.FeedBack = Math.Max(Math.Min(FeedBackMax, value), FeedBackMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Frequency of the LFO.
            /// </summary>
            public float Frequency
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Frequency;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Frequency = Math.Max(Math.Min(FrequencyMax, value), FrequencyMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Waveform shape of the LFO.
            /// </summary>
            public FlangerWaveForm WaveForm
            {
                get
                {
                    var param = GetAllParameters();
                    return param.WaveForm;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(FlangerWaveForm), value))
                    {
                        param.WaveForm = value;
                    }
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Number of milliseconds the input is delayed before it is played back.
            /// </summary>
            public float Delay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Delay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Delay = Math.Max(Math.Min(DelayMax, value), DelayMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Phase differential between left and right LFOs.
            /// </summary>
            public FlangerPhase Phase
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Phase;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(FlangerPhase), value))
                    {
                        param.Phase = value;
                    }
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXFlanger fxFlanger;

            internal Params(IDirectSoundFXFlanger dsFxObject)
            {
                fxFlanger = dsFxObject;
            }

            private void SetAllParameters(DsFxFlanger param)
            {
                Marshal.ThrowExceptionForHR(fxFlanger.SetAllParameters(ref param));
            }

            private DsFxFlanger GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxFlanger.GetAllParameters(out var param));
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
        /// Create new DMO Flanger
        /// </summary>
        public DmoFlanger()
        {
            var guidFlanger = new Guid("EFCA3D92-DFD8-4672-A603-7420894BAD98");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidFlanger));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject)mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace)mediaComObject);
                effectParams = new Params((IDirectSoundFXFlanger)mediaComObject);
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