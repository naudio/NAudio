using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.Dmo.Effect
{
    internal struct DsFxEcho
    {
        public float WetDryMix;
        public float FeedBack;
        public float LeftDelay;
        public float RightDelay;
        public EchoPanDelay PanDelay;
    }

    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     Guid("8bd28edf-50db-4e92-a2bd-445488d1ed42"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDirectSoundFXEcho
    {
        [PreserveSig]
        int SetAllParameters([In] ref DsFxEcho param);

        [PreserveSig]
        int GetAllParameters(out DsFxEcho param);
    }

    /// <summary>
    /// Dmo Echo Effect
    /// </summary>
    public class DmoEcho : IDmoEffector<DmoEcho.Params>
    {
        /// <summary>
        /// DMO Echo Params
        /// </summary>
        public struct Params
        {
            /// <summary>
            /// DSFXECHO_WETDRYMIX_MIN
            /// </summary>
            public const float WetDryMixMin = 0.0f;
            /// <summary>
            /// DSFXECHO_WETDRYMIX_MAX
            /// </summary>
            public const float WetDryMixMax = 100.0f;
            /// <summary>
            /// DSFXECHO_WETDRYMIX_DEFAULT
            /// </summary>
            public const float WetDeyMixDefault = 50.0f;

            /// <summary>
            /// DSFXECHO_FEEDBACK_MIN
            /// </summary>
            public const float FeedBackMin = 0.0f;
            /// <summary>
            /// DSFXECHO_FEEDBACK_MAX
            /// </summary>
            public const float FeedBackMax = 100.0f;
            /// <summary>
            /// DSFXECHO_FEEDBACK_DEFAULT
            /// </summary>
            public const float FeedBackDefault = 50.0f;

            /// <summary>
            /// DSFXECHO_LEFTDELAY_MIN
            /// </summary>
            public const float LeftDelayMin = 1.0f;
            /// <summary>
            /// DSFXECHO_LEFTDELAY_MAX
            /// </summary>
            public const float LeftDelayMax = 2000.0f;
            /// <summary>
            /// DSFXECHO_LEFTDELAY_DEFAULT
            /// </summary>
            public const float LeftDelayDefault = 500.0f;

            /// <summary>
            /// DSFXECHO_RIGHTDELAY_MIN
            /// </summary>
            public const float RightDelayMin = 1.0f;
            /// <summary>
            /// DSFXECHO_RIGHTDELAY_MAX
            /// </summary>
            public const float RightDelayMax = 2000.0f;
            /// <summary>
            /// DSFXECHO_RIGHTDELAY_DEFAULT
            /// </summary>
            public const float RightDelayDefault = 500.0f;

            /// <summary>
            /// DSFXECHO_PANDELAY_DEFAULT
            /// </summary>
            public const EchoPanDelay PanDelayDefault = EchoPanDelay.Off;

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
            /// Percentage of output fed back into input.
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
            /// Delay for left channel, in milliseconds.
            /// </summary>
            public float LeftDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.LeftDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.LeftDelay = Math.Max(Math.Min(LeftDelayMax, value), LeftDelayMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Delay for right channel, in milliseconds.
            /// </summary>
            public float RightDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.RightDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    param.RightDelay = Math.Max(Math.Min(RightDelayMax, value), RightDelayMin);
                    SetAllParameters(param);
                }
            }

            /// <summary>
            /// Value that specifies whether to swap left and right delays with each successive echo.
            /// </summary>
            public EchoPanDelay PanDelay
            {
                get
                {
                    var param = GetAllParameters();
                    return param.PanDelay;
                }
                set
                {
                    var param = GetAllParameters();
                    if (Enum.IsDefined(typeof(EchoPanDelay), value))
                    {
                        param.PanDelay = value;
                    }
                    SetAllParameters(param);
                }
            }

            private readonly IDirectSoundFXEcho fxEcho;

            internal Params(IDirectSoundFXEcho dsFxObject)
            {
                fxEcho = dsFxObject;
            }

            private void SetAllParameters(DsFxEcho param)
            {
                Marshal.ThrowExceptionForHR(fxEcho.SetAllParameters(ref param));
            }

            private DsFxEcho GetAllParameters()
            {
                Marshal.ThrowExceptionForHR(fxEcho.GetAllParameters(out var param));
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
        /// Create new DMO Echo
        /// </summary>
        public DmoEcho()
        {
            var guidEcho = new Guid("EF3E932C-D40B-4F51-8CCF-3F98F1B29D5D");

            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guidEcho));

            if (targetDescriptor != null)
            {
                var mediaComObject = Activator.CreateInstance(Type.GetTypeFromCLSID(targetDescriptor.Clsid));

                mediaObject = new MediaObject((IMediaObject) mediaComObject);
                mediaObjectInPlace = new MediaObjectInPlace((IMediaObjectInPlace) mediaComObject);
                effectParams = new Params((IDirectSoundFXEcho) mediaComObject);
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