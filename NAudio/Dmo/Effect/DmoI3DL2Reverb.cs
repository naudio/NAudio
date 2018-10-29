using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxI3Dl2Reverb
    {
        public int Room;
        public int RoomHf;
        public float RoomRollOffFactor;
        public float DecayTime;
        public float DecayHfRatio;
        public int Reflections;
        public float ReflectionsDelay;
        public int Reverb;
        public float ReverbDelay;
        public float Diffusion;
        public float Density;
        public float HfReference;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("4b166a6a-0d66-43f3-80e3-ee6280dee1a4"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXI3DL2Reverb
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxI3Dl2Reverb param);

        [PreserveSig]
        int GetAllParameters(out DsFxI3Dl2Reverb param);

        [PreserveSig]
        int SetPreset([In] uint preset);

        [PreserveSig]
        int GetPreset(out uint preset);

        [PreserveSig]
        int SetQuality([In] int quality);

        [PreserveSig]
        int GetQuality(out int quality);
    }

    /// <summary>
    /// DMO I3DL2Reverb Effect
    /// </summary>
    public class DmoI3DL2Reverb : IDmoEffector<DmoI3DL2Reverb.Params>
    {
        /// <summary>
        /// DMO I3DL2Reverb Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOM_MIN
            /// </summary>
            public const int RoomMin = -10000;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOM_MAX
            /// </summary>
            public const int RoomMax = 0;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOM_DEFAULT
            /// </summary>
            public const int RoomDefault = -1000;

            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMHF_MIN
            /// </summary>
            public const int RoomHfMin = -10000;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMHF_MAX
            /// </summary>
            public const int RoomHfMax = 0;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMHF_DEFAULT
            /// </summary>
            public const int RoomHfDefault = -100;

            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMROLLOFFFACTOR_MIN
            /// </summary>
            public const float RoomRollOffFactorMin = 0.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMROLLOFFFACTOR_MAX
            /// </summary>
            public const float RoomRollOffFactorMax = 10.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_ROOMROLLOFFFACTOR_DEFAULT
            /// </summary>
            public const float RoomRollOffFactorDefault = 0.0f;

            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYTIME_MIN
            /// </summary>
            public const float DecayTimeMin = 0.1f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYTIME_MAX
            /// </summary>
            public const float DecayTimeMax = 20.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYTIME_DEFAULT
            /// </summary>
            public const float DecayTimeDefault = 1.49f;

            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYHFRATIO_MIN
            /// </summary>
            public const float DecayHfRatioMin = 0.1f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYHFRATIO_MAX
            /// </summary>
            public const float DecayHfRatioMax = 2.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DECAYHFRATIO_DEFAULT
            /// </summary>
            public const float DecayHfRatioDefault = 0.83f;

            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONS_MIN
            /// </summary>
            public const int ReflectionsMin = -10000;
            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONS_MAX
            /// </summary>
            public const int ReflectionsMax = 1000;
            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONS_DEFAULT
            /// </summary>
            public const int ReflectionsDefault = -2602;

            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONSDELAY_MIN
            /// </summary>
            public const float ReflectionsDelayMin = 0.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONSDELAY_MAX
            /// </summary>
            public const float ReflectionsDelayMax = 0.3f;
            /// <summary>
            /// DSFX_I3DL2REVERB_REFLECTIONSDELAY_DEFAULT
            /// </summary>
            public const float ReflectionsDelayDefault = 0.007f;

            /// <summary>
            /// DSFX_I3DL2REVERB_REVERB_MIN
            /// </summary>
            public const int ReverbMin = -10000;
            /// <summary>
            /// DSFX_I3DL2REVERB_REVERB_MAX
            /// </summary>
            public const int ReverbMax = 2000;
            /// <summary>
            /// DSFX_I3DL2REVERB_REVERB_DEFAULT
            /// </summary>
            public const int ReverbDefault = 200;

            /// <summary>
            /// DSFX_I3DL2REVERB_REVERBDELAY_MIN
            /// </summary>
            public const float ReverbDelayMin = 0.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_REVERBDELAY_MAX
            /// </summary>
            public const float ReverbDelayMax = 0.1f;
            /// <summary>
            /// DSFX_I3DL2REVERB_REVERBDELAY_DEFAULT
            /// </summary>
            public const float ReverbDelayDefault = 0.011f;

            /// <summary>
            /// DSFX_I3DL2REVERB_DIFFUSION_MIN
            /// </summary>
            public const float DiffusionMin = 0.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DIFFUSION_MAX
            /// </summary>
            public const float DiffusionMax = 100.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DIFFUSION_DEFAULT
            /// </summary>
            public const float DiffusionDefault = 100.0f;

            /// <summary>
            /// DSFX_I3DL2REVERB_DENSITY_MIN
            /// </summary>
            public const float DensityMin = 0.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DENSITY_MAX
            /// </summary>
            public const float DensityMax = 100.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_DENSITY_DEFAULT
            /// </summary>
            public const float DensityDefault = 100.0f;

            /// <summary>
            /// DSFX_I3DL2REVERB_HFREFERENCE_MIN
            /// </summary>
            public const float HfReferenceMin = 20.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_HFREFERENCE_MAX
            /// </summary>
            public const float HfReferenceMax = 20000.0f;
            /// <summary>
            /// DSFX_I3DL2REVERB_HFREFERENCE_DEFAULT
            /// </summary>
            public const float HfReferenceDefault = 5000.0f;

            /// <summary>
            /// DSFX_I3DL2REVERB_QUALITY_MIN
            /// </summary>
            public const int QualityMin = 0;
            /// <summary>
            /// DSFX_I3DL2REVERB_QUALITY_MAX
            /// </summary>
            public const int QualityMax = 3;
            /// <summary>
            /// DSFX_I3DL2REVERB_QUALITY_DEFAULT
            /// </summary>
            public const int QualityDefault = 2;

            /// <summary>
            /// Attenuation of the room effect, in millibels (mB)
            /// </summary>
            public int Room
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Room;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Room = Math.Max(Math.Min(RoomMax, value), RoomMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Attenuation of the room high-frequency effect, in mB.
            /// </summary>
            public int RoomHf
            {
                get
                {
                    var param = GetAllParameters();
                    return param.RoomHf;
                }
                set
                {
                    var param = GetAllParameters();
                    param.RoomHf = Math.Max(Math.Min(RoomHfMax, value), RoomHfMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Rolloff factor for the reflected signals.
            /// </summary>
            public float RoomRollOffFactor
            {
                get
                {
                    var param = GetAllParameters();
                    return param.RoomRollOffFactor;
                }
                set
                {
                    var param = GetAllParameters();
                    param.RoomRollOffFactor = Math.Max(Math.Min(RoomRollOffFactorMax, value), RoomRollOffFactorMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Decay time, in seconds.
            /// </summary>
            public float DecayTime
            {
                get
                {
                    var param = GetAllParameters();
                    return param.DecayTime;
                }
                set
                {
                    var param = GetAllParameters();
                    param.DecayTime = Math.Max(Math.Min(DecayTimeMax, value), DecayTimeMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Ratio of the decay time at high frequencies to the decay time at low frequencies.
            /// </summary>
            public float DecayHfRatio
            {
                get
                {
                    var param = GetAllParameters();
                    return param.DecayHfRatio;
                }
                set
                {
                    var param = GetAllParameters();
                    param.DecayHfRatio = Math.Max(Math.Min(DecayHfRatioMax, value), DecayHfRatioMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Attenuation of early reflections relative to lRoom, in mB.
            /// </summary>
            public int Reflections
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Reflections;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Reflections = Math.Max(Math.Min(ReflectionsMax, value), ReflectionsMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Delay time of the first reflection relative to the direct path, in seconds.
            /// </summary>
            public float ReflectionsDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.ReflectionsDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.ReflectionsDelay = Math.Max(Math.Min(ReflectionsDelayMax, value), ReflectionsDelayMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Attenuation of late reverberation relative to lRoom, in mB.
            /// </summary>
            public int Reverb
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Reverb;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Reverb = Math.Max(Math.Min(ReverbMax, value), ReverbMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Time limit between the early reflections and the late reverberation relative to the time of the first reflection.
            /// </summary>
            public float ReverbDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.ReverbDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.ReverbDelay = Math.Max(Math.Min(ReverbDelayMax, value), ReverbDelayMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Echo density in the late reverberation decay, in percent.
            /// </summary>
            public float Diffusion
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Diffusion;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Diffusion = Math.Max(Math.Min(DiffusionMax, value), DiffusionMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Modal density in the late reverberation decay, in percent.
            /// </summary>
            public float Density
            {
                get
                {
                    var param = GetAllParameters();
                    return param.Density;
                }
                set
                {
                    var param = GetAllParameters();
                    param.Density = Math.Max(Math.Min(DensityMax, value), DensityMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Reference high frequency, in hertz.
            /// </summary>
            public float HfReference
            {
                get
                {
                    var param = GetAllParameters();
                    return param.HfReference;
                }
                set
                {
                    var param = GetAllParameters();
                    param.HfReference = Math.Max(Math.Min(HfReferenceMax, value), HfReferenceMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// the quality of the environmental reverberation effect. Higher values produce better quality at the expense of processing time.
            /// </summary>
            public int Quality
            {
                get
                {
                    Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.GetQuality(out var quality));
                    return quality;
                }
                set => Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.SetQuality(value));
            }

            private readonly IDirectSoundFXI3DL2Reverb fxI3Dl2Reverb;

            internal Params(IDirectSoundFXI3DL2Reverb dsFxObject)
            {
                fxI3Dl2Reverb = dsFxObject;
            }

            /// <summary>
            /// Sets standard reverberation parameters of a buffer.
            /// </summary>
            /// <param name="preset">I3DL2EnvironmentPreset</param>
            public void SetPreset(I3DL2EnvironmentPreset preset)
            {
                var p = (uint)preset;
                Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.SetPreset(p));
            }

            /// <summary>
            /// retrieves an identifier for standard reverberation parameters of a buffer.
            /// </summary>
            /// <returns>I3DL2EnvironmentPreset</returns>
            public I3DL2EnvironmentPreset GetPreset()
            {
                Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.GetPreset(out var preset));
                return (I3DL2EnvironmentPreset)preset;
            }

            private void SetAllParameters(DsFxI3Dl2Reverb param)
            {
                Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.SetAllParameters(ref param));
            }

            private DsFxI3Dl2Reverb GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxI3Dl2Reverb.GetAllParameters(out var param));
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
        /// Create new DMO I3DL2Reverb
        /// </summary>
        public DmoI3DL2Reverb()
        {
            var guidi3Dl2Reverb = new Guid("EF985E71-D5C7-42D4-BA4D-2D073E2E96F4");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidi3Dl2Reverb));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject)mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace)mediaComObject);
                effectParams = new Params((IDirectSoundFXI3DL2Reverb)mediaComObject);
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