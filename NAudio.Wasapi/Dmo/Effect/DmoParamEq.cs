using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxParamEq
    {
        public float Center;
        public float BandWidth;
        public float Gain;
    }

    [ComImport,
     System.Security.SuppressUnmanagedCodeSecurity,
     Guid("c03ca9fe-fe90-4204-8078-82334cd177da"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFxParamEq
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxParamEq param);

        [PreserveSig]
        int GetAllParameters(out DsFxParamEq param);
    }

    /// <summary>
    /// DMO Parametric Equalizer Effect
    /// </summary>
    public class DmoParamEq : IDmoEffector<DmoParamEq.Params>
    {
        private static readonly Guid Id_ParamEq = new Guid("120CED89-3BF4-4173-A132-3CB406CF3231");

        /// <summary>
        /// DMO ParamEq Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXPARAMEQ_CENTER_MIN
            /// </summary>
            public const float CenterMin = 80.0f;
            /// <summary>
            /// DSFXPARAMEQ_CENTER_MAX
            /// </summary>
            public const float CenterMax = 16000.0f;
            /// <summary>
            /// DSFXPARAMEQ_CENTER_DEFAULT
            /// </summary>
            public const float CenterDefault = 8000.0f;

            /// <summary>
            /// DSFXPARAMEQ_BANDWIDTH_MIN
            /// </summary>
            public const float BandWidthMin = 1.0f;
            /// <summary>
            /// DSFXPARAMEQ_BANDWIDTH_MAX
            /// </summary>
            public const float BandWidthMax = 36.0f;
            /// <summary>
            /// DSFXPARAMEQ_BANDWIDTH_DEFAULT
            /// </summary>
            public const float BandWidthDefault = 12.0f;

            /// <summary>
            /// DSFXPARAMEQ_GAIN_MIN
            /// </summary>
            public const float GainMin = -15.0f;
            /// <summary>
            /// DSFXPARAMEQ_GAIN_MAX
            /// </summary>
            public const float GainMax = 15.0f;
            /// <summary>
            /// DSFXPARAMEQ_GAIN_DEFAULT
            /// </summary>
            public const float GainDefault = 0.0f;

            /// <summary>
            /// Center frequency, in hertz
            /// </summary>
            public float Center
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Center;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Center = Math.Max(Math.Min(CenterMax, value), CenterMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Bandwidth, in semitones.
            /// </summary>
            public float BandWidth
            {
                get
                {
                    var param = GetAllParameters();
                    return param.BandWidth;
                }
                set
                {
                    var param = GetAllParameters();
                    param.BandWidth = Math.Max(Math.Min(BandWidthMax, value), BandWidthMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Gain
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

            private readonly IDirectSoundFxParamEq fxParamEq;

            internal Params(IDirectSoundFxParamEq dsFxObject)
            {
                fxParamEq = dsFxObject;
            }

            private void SetAllParameters(DsFxParamEq param)
            {
                Marshal.ThrowExceptionForHR(fxParamEq.SetAllParameters(ref param));
            }

            private DsFxParamEq GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxParamEq.GetAllParameters(out var param));
                return param;
            }
        }

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject { get; }

        /// <summary>
        /// Media Object InPlace
        /// </summary>
        public MediaObjectInPlace MediaObjectInPlace { get; }

        /// <summary>
        /// Effect Parameter
        /// </summary>
        public Params EffectParams { get; }

        /// <summary>
        /// Create new DMO ParamEq
        /// </summary>
        public DmoParamEq()
        {
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, Id_ParamEq));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                MediaObject = new MediaObject((IMediaObject) mediaComObject);
                MediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace) mediaComObject);
                EffectParams = new Params((IDirectSoundFxParamEq) mediaComObject);
            }
        }

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose()
        {
            MediaObjectInPlace?.Dispose();
            MediaObject?.Dispose();
        }
    }
}