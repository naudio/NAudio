using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxChorus
    {
        public float WetDryMix;
        public float Depth;
        public float FeedBack;
        public float Frequency;
        public ChorusWaveForm WaveForm;
        public float Delay;
        public ChorusPhase Phase;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("880842e3-145f-43e6-a934-a71806e50547"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDirectSoundFXChorus
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxChorus param);

        [PreserveSig]
        int GetAllParameters(out DsFxChorus param);
    }

    /// <summary>
    /// DMO Chorus Effect
    /// </summary>
    public class DmoChorus : IDmoEffector<DmoChorus.Params>
    {
        /// <summary>
        /// DMO Chorus Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXCHORUS_WETDRYMIX_MIN
            /// </summary>
            public const float WetDryMixMin = 0.0f;
            /// <summary>
            /// DSFXCHORUS_WETDRYMIX_MAX
            /// </summary>
            public const float WetDryMixMax = 100.0f;
            /// <summary>
            /// DSFXCHORUS_WETDRYMIX_DEFAULT
            /// </summary>
            public const float WetDrtMixDefault = 50.0f;

            /// <summary>
            /// DSFXCHORUS_DEPTH_MIN
            /// </summary>
            public const float DepthMin = 0.0f;
            /// <summary>
            /// DSFXCHORUS_DEPTH_MAX
            /// </summary>
            public const float DepthMax = 100.0f;
            /// <summary>
            /// DSFXCHORUS_DEPTH_DEFAULT
            /// </summary>
            public const float DepthDefault = 10.0f;

            /// <summary>
            /// DSFXCHORUS_FEEDBACK_MIN
            /// </summary>
            public const float FeedBackMin = -99.0f;
            /// <summary>
            /// DSFXCHORUS_FEEDBACK_MAX
            /// </summary>
            public const float FeedBackMax = 99.0f;
            /// <summary>
            /// DSFXCHORUS_FEEDBACK_DEFAULT
            /// </summary>
            public const float FeedBaclDefault = 25.0f;

            /// <summary>
            /// DSFXCHORUS_FREQUENCY_MIN
            /// </summary>
            public const float FrequencyMin = 0.0f;
            /// <summary>
            /// DSFXCHORUS_FREQUENCY_MAX
            /// </summary>
            public const float FrequencyMax = 10.0f;
            /// <summary>
            /// DSFXCHORUS_FREQUENCY_DEFAULT
            /// </summary>
            public const float FrequencyDefault = 1.1f;

            /// <summary>
            /// DSFXCHORUS_WAVE_DEFAULT
            /// </summary>
            public const ChorusWaveForm WaveFormDefault = ChorusWaveForm.Sin;

            /// <summary>
            /// DSFXCHORUS_DELAY_MIN
            /// </summary>
            public const float DelayMin = 0.0f;
            /// <summary>
            /// DSFXCHORUS_DELAY_MAX
            /// </summary>
            public const float DelayMax = 20.0f;
            /// <summary>
            /// DSFXCHORUS_DELAY_DEFAULT
            /// </summary>
            public const float DelayDefault = 16.0f;

            /// <summary>
            /// DSFXCHORUS_PHASE_DEFAULT
            /// </summary>
            public const ChorusPhase PhaseDefault = ChorusPhase.Pos90;

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
            public ChorusWaveForm WaveForm
            {
                get
                {
                    var param = GetAllParameters();
                    return param.WaveForm;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(ChorusWaveForm), value))
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
            public ChorusPhase Phase
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Phase;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(ChorusPhase), value))
                    {
                        param.Phase = value;
                    }
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXChorus fxChorus;

            internal Params(IDirectSoundFXChorus dsFxObject)
            {
                fxChorus = dsFxObject;
            }

            private void SetAllParameters(DsFxChorus param)
            {
                Marshal.ThrowExceptionForHR(fxChorus.SetAllParameters(ref param));
            }

            private DsFxChorus GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxChorus.GetAllParameters(out var param));
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
        /// Create new DMO Chorus
        /// </summary>
        public DmoChorus()
        {
            var guidChorus = new Guid("EFE6629C-81F7-4281-BD91-C9D604A95AF6");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidChorus));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject) mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace) mediaComObject);
                effectParams = new Params((IDirectSoundFXChorus) mediaComObject);
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