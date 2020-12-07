using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxGargle
    {
        public uint RateHz;
        public GargleWaveShape WaveShape;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("d616f352-d622-11ce-aac5-0020af0b99a3"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXGargle
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxGargle param);

        [PreserveSig]
        int GetAllParameters(out DsFxGargle param);
    }

    /// <summary>
    /// DMO Gargle Effect
    /// </summary>
    public class DmoGargle : IDmoEffector<DmoGargle.Params>
    {
        /// <summary>
        /// DMO Gargle Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXGARGLE_RATEHZ_MIN
            /// </summary>
            public const uint RateHzMin = 1;
            /// <summary>
            /// DSFXGARGLE_RATEHZ_MAX
            /// </summary>
            public const uint RateHzMax = 1000;
            /// <summary>
            /// DSFXGARGLE_RATEHZ_DEFAULT
            /// </summary>
            public const uint RateHzDefault = 20;

            /// <summary>
            /// DSFXGARGLE_WAVE_DEFAULT
            /// </summary>
            public const GargleWaveShape WaveShapeDefault = GargleWaveShape.Triangle;

            /// <summary>
            /// Rate of modulation in hz
            /// </summary>
            public uint RateHz
            {
                get
                {
                    var param = GetAllParameters();
                    return param.RateHz;
                }
                set
                {
                    var param = GetAllParameters();
                    param.RateHz = Math.Max(Math.Min(RateHzMax, value), RateHzMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Gargle Wave Shape
            /// </summary>
            public GargleWaveShape WaveShape
            {
                get
                {
                    var param = GetAllParameters();
                    return param.WaveShape;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(GargleWaveShape), value))
                    {
                        param.WaveShape = value;
                    }
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXGargle fxGargle;

            internal Params(IDirectSoundFXGargle dsFxObject)
            {
                fxGargle = dsFxObject;
            }

            private void SetAllParameters(DsFxGargle param)
            {
                Marshal.ThrowExceptionForHR(fxGargle.SetAllParameters(ref param));
            }

            private DsFxGargle GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxGargle.GetAllParameters(out var param));
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
        /// Create new DMO Gargle
        /// </summary>
        public DmoGargle()
        {
            var guidGargle = new Guid("DAFD8210-5711-4B91-9FE3-F75B7AE279BF");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidGargle));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject) mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace) mediaComObject);
                effectParams = new Params((IDirectSoundFXGargle) mediaComObject);
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