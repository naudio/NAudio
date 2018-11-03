using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxDistortion
    {
        public float Gain;
        public float Edge;
        public float PostEqCenterFrequency;
        public float PostEqBandWidth;
        public float PreLowPassCutoff;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("8ecf4326-455f-4d8b-bda9-8d5d3e9e3e0b"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXDistortion
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxDistortion param);

        [PreserveSig]
        int GetAllParameters(out DsFxDistortion param);
    }

    /// <summary>
    /// DMO Distortion Effect
    /// </summary>
    public class DmoDistortion : IDmoEffector<DmoDistortion.Params>
    {
        /// <summary>
        /// DMO Distortion Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXDISTORTION_GAIN_MIN
            /// </summary>
            public const float GainMin = -60.0f;
            /// <summary>
            /// DSFXDISTORTION_GAIN_MAX
            /// </summary>
            public const float GainMax = 0.0f;
            /// <summary>
            /// DSFXDISTORTION_GAIN_DEFAULT
            /// </summary>
            public const float GainDefault = -18.0f;

            /// <summary>
            /// DSFXDISTORTION_EDGE_MIN
            /// </summary>
            public const float EdgeMin = 0.0f;
            /// <summary>
            /// DSFXDISTORTION_EDGE_MAX
            /// </summary>
            public const float EdgeMax = 100.0f;
            /// <summary>
            /// DSFXDISTORTION_EDGE_DEFAULT
            /// </summary>
            public const float EdgeDefault = 15.0f;

            /// <summary>
            /// DSFXDISTORTION_POSTEQCENTERFREQUENCY_MIN
            /// </summary>
            public const float PostEqCenterFrequencyMin = 100.0f;
            /// <summary>
            /// DSFXDISTORTION_POSTEQCENTERFREQUENCY_MAX
            /// </summary>
            public const float PostEqCenterFrequencyMax = 8000.0f;
            /// <summary>
            /// DSFXDISTORTION_POSTEQCENTERFREQUENCY_DEFAULT
            /// </summary>
            public const float PostEqCenterFrequencyDefault = 2400.0f;

            /// <summary>
            /// DSFXDISTORTION_POSTEQBANDWIDTH_MIN
            /// </summary>
            public const float PostEqBandWidthMin = 100.0f;
            /// <summary>
            /// DSFXDISTORTION_POSTEQBANDWIDTH_MAX
            /// </summary>
            public const float PostEqBandWidthMax = 8000.0f;
            /// <summary>
            /// DSFXDISTORTION_POSTEQBANDWIDTH_DEFAULT
            /// </summary>
            public const float PostEqBandWidthDefault = 2400.0f;

            /// <summary>
            /// DSFXDISTORTION_PRELOWPASSCUTOFF_MIN
            /// </summary>
            public const float PreLowPassCutoffMin = 100.0f;
            /// <summary>
            /// DSFXDISTORTION_PRELOWPASSCUTOFF_MAX
            /// </summary>
            public const float PreLowPassCutoffMax = 8000.0f;
            /// <summary>
            /// DSFXDISTORTION_PRELOWPASSCUTOFF_DEFAULT
            /// </summary>
            public const float PreLowPassCutoffDefault = 8000.0f;

            /// <summary>
            /// Amount of signal change after distortion.
            /// </summary>
            public float Gain
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Gain;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Gain = Math.Max(Math.Min(GainMax, value), GainMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Percentage of distortion intensity.
            /// </summary>
            public float Edge
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Edge;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Edge = Math.Max(Math.Min(EdgeMax, value), EdgeMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Center frequency of harmonic content addition.
            /// </summary>
            public float PostEqCenterFrequency
            {
                get
                {
                    var param = GetAllParameters();
                    return param.PostEqCenterFrequency;
                }
                set
                {
                    var param = GetAllParameters();
                    param.PostEqCenterFrequency = Math.Max(Math.Min(PostEqCenterFrequencyMax, value), PostEqCenterFrequencyMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Width of frequency band that determines range of harmonic content addition.
            /// </summary>
            public float PostEqBandWidth
            {
                get
                {
                    var param = GetAllParameters();
                    return param.PostEqBandWidth;
                }
                set
                {
                    var param = GetAllParameters();
                    param.PostEqBandWidth = Math.Max(Math.Min(PostEqBandWidthMax, value), PostEqBandWidthMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Filter cutoff for high-frequency harmonics attenuation.
            /// </summary>
            public float PreLowPassCutoff
            {
                get
                {
                    var param = GetAllParameters();
                    return param.PreLowPassCutoff;
                }
                set
                {
                    var param = GetAllParameters();
                    param.PreLowPassCutoff = Math.Max(Math.Min(PreLowPassCutoffMax, value), PreLowPassCutoffMin);
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXDistortion fxDistortion;

            internal Params(IDirectSoundFXDistortion dsFxObject)
            {
                fxDistortion = dsFxObject;
            }

            private void SetAllParameters(DsFxDistortion param)
            {
                Marshal.ThrowExceptionForHR(fxDistortion.SetAllParameters(ref param));
            }

            private DsFxDistortion GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxDistortion.GetAllParameters(out var param));
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
        /// Create new DMO Distortion
        /// </summary>
        public DmoDistortion()
        {
            var guidDistortion = new Guid("EF114C90-CD1D-484E-96E5-09CFAF912A21");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidDistortion));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject)mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace)mediaComObject);
                effectParams = new Params((IDirectSoundFXDistortion)mediaComObject);
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