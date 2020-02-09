using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxCompressor
    {
        public float Gain;
        public float Attack;
        public float Release;
        public float Threshold;
        public float Ratio;
        public float PreDelay;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("4bbd1154-62f6-4e2c-a15c-d3b6c417f7a0"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXCompressor
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxCompressor param);

        [PreserveSig]
        int GetAllParameters(out DsFxCompressor param);
    }

    /// <summary>
    /// DMO Compressor Effect
    /// </summary>
    public class DmoCompressor : IDmoEffector<DmoCompressor.Params>
    {
        /// <summary>
        /// DMO Compressor Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXCOMPRESSOR_GAIN_MIN
            /// </summary>
            public const float GainMin = -60.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_GAIN_MAX
            /// </summary>
            public const float GainMax = 60.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_GAIN_DEFAULT
            /// </summary>
            public const float GainDefault = 0.0f;

            /// <summary>
            /// DSFXCOMPRESSOR_ATTACK_MIN
            /// </summary>
            public const float AttackMin = 0.01f;
            /// <summary>
            /// DSFXCOMPRESSOR_ATTACK_MAX
            /// </summary>
            public const float AttackMax = 500.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_ATTACK_DEFAULT
            /// </summary>
            public const float AttackDefault = 10.0f;

            /// <summary>
            /// DSFXCOMPRESSOR_RELEASE_MIN
            /// </summary>
            public const float ReleaseMin = 50.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_RELEASE_MAX
            /// </summary>
            public const float ReleaseMax = 3000.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_RELEASE_DEFAULT
            /// </summary>
            public const float ReleaseDefault = 200.0f;

            /// <summary>
            /// DSFXCOMPRESSOR_THRESHOLD_MIN
            /// </summary>
            public const float ThresholdMin = -60.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_THRESHOLD_MAX
            /// </summary>
            public const float ThresholdMax = 0.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_THRESHOLD_DEFAULT
            /// </summary>
            public const float TjresholdDefault = -20.0f;

            /// <summary>
            /// DSFXCOMPRESSOR_RATIO_MIN
            /// </summary>
            public const float RatioMin = 1.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_RATIO_MAX
            /// </summary>
            public const float RatioMax = 100.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_RATIO_DEFAULT
            /// </summary>
            public const float RatioDefault = 3.0f;

            /// <summary>
            /// DSFXCOMPRESSOR_PREDELAY_MIN
            /// </summary>
            public const float PreDelayMin = 0.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_PREDELAY_MAX
            /// </summary>
            public const float PreDelayMax = 4.0f;
            /// <summary>
            /// DSFXCOMPRESSOR_PREDELAY_DEFAULT
            /// </summary>
            public const float PreDelayDefault = 4.0f;

            /// <summary>
            /// Output gain of signal after compression.
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
            /// Time before compression reaches its full value.
            /// </summary>
            public float Attack
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Attack;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Attack = Math.Max(Math.Min(AttackMax, value), AttackMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Speed at which compression is stopped after input drops below Threshold.
            /// </summary>
            public float Release
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Release;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Release = Math.Max(Math.Min(ReleaseMax, value), ReleaseMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Point at which compression begins, in decibels.
            /// </summary>
            public float Threshold
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Threshold;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Threshold = Math.Max(Math.Min(ThresholdMax, value), ThresholdMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Compression ratio
            /// </summary>
            public float Ratio
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Ratio;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Ratio = Math.Max(Math.Min(RatioMax, value), RatioMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Time after Threshold is reached before attack phase is started, in milliseconds.
            /// </summary>
            public float PreDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.PreDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.PreDelay = Math.Max(Math.Min(PreDelayMax, value), PreDelayMin);
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXCompressor fxCompressor;

            internal Params(IDirectSoundFXCompressor dsFxObject)
            {
                fxCompressor = dsFxObject;
            }

            private void SetAllParameters(DsFxCompressor param)
            {
                Marshal.ThrowExceptionForHR(fxCompressor.SetAllParameters(ref param));
            }

            private DsFxCompressor GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxCompressor.GetAllParameters(out var param));
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
        /// Create new DMO Compressor
        /// </summary>
        public DmoCompressor()
        {
            var guidChorus = new Guid("EF011F79-4000-406D-87AF-BFFB3FC39D57");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidChorus));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject)mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace)mediaComObject);
                effectParams = new Params((IDirectSoundFXCompressor)mediaComObject);
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