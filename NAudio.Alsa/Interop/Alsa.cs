using System;
using System.Runtime.InteropServices;
namespace NAudio.Wave.Alsa
{
    internal class AlsaInterop
    {
        public delegate void PcmCallback(IntPtr handler);
        private const string AlsaLibrary = "libasound";
        [DllImport(AlsaLibrary, EntryPoint = "snd_card_next")]
        internal static extern int NextCard(ref int rcard);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_open")]
        internal static extern int CtlOpen(out IntPtr ctlp, string name, int mode);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_close")]
        internal static extern int CtlClose(IntPtr ctl);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_card_info")]
        internal static unsafe extern int CtlCardInfo(IntPtr ctlp, IntPtr info);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_card_info_free")]
        internal static unsafe extern int CtlCardInfoFree(IntPtr info);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_card_info_get_card")] 
        internal static extern int CtlCardInfoGetCard(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_id")] 
        private static extern IntPtr ctlCardInfoGetID(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_driver")] 
        private static extern IntPtr ctlCardInfoGetDriver(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_name")] 
        private static extern IntPtr ctlCardInfoGetName(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_longname")] 
        private static extern IntPtr ctlCardInfoGetLongName(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_mixername")] 
        private static extern IntPtr ctlCardInfoGetMixerName(IntPtr info);
        [DllImport(AlsaLibrary, CharSet = CharSet.Ansi, EntryPoint = "snd_ctl_card_info_get_components")] 
        private static extern IntPtr ctlCardInfoGetComponents(IntPtr info);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_card_info_malloc")]
        internal static unsafe extern void CtlCardInfoMalloc(ref IntPtr info);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_malloc")]
        internal static extern void PcmInfoMalloc(out IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_free")]
        internal static extern void PcmInfoFree(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_stream_name")]
        internal static extern string PcmStreamName(IntPtr stream);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_pcm_next_device")]
        internal static extern int CtlPcmNextDevice(IntPtr ctl, ref int device);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_set_device")]
        internal static extern void PcmInfoSetDevice(IntPtr pcmInfo, uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_set_subdevice")]
        internal static extern void PcmInfoSetSubdevice(IntPtr pcmInfo, uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_set_stream")]
        internal static extern void PcmInfoSetStream(IntPtr pcmInfo, PCMStream stream);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_id")]
        private static extern IntPtr pcmInfoGetID(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_name")]
        private static extern IntPtr pcmInfoGetName(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_stream")]
        internal static extern PCMStream PcmInfoGetStream(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_subdevice_name")]
        private static extern IntPtr pcmInfoGetSubdeviceName(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_subdevices_avail")]
        internal static extern uint PcmInfoGetSubdevicesAvailable(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_ctl_pcm_info")]
        internal static extern int CtlPcmInfo(IntPtr ctl, IntPtr info);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_info_get_subdevices_count")]
        internal static extern uint PcmInfoGetSubdevicesCount(IntPtr pcmInfo);
        [DllImport(AlsaLibrary, EntryPoint = "snd_strerror")]
        private static extern IntPtr strError(int error);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_open")]
        internal static extern int PcmOpen(out IntPtr pcm, string name, PCMStream stream, int mode);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_close")]
        internal static extern int PcmClose(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_malloc")]
        internal static unsafe extern int PcmHwParamsMalloc(out IntPtr hwparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_free")]
        internal static unsafe extern void PcmHwParamsFree(IntPtr hwparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_test_access")]
        internal static extern int PcmHwParamsTestAccess(IntPtr pcm, IntPtr hwparams, PCMAccess access);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_any")]
        internal static extern int PcmHwParamsAny(IntPtr pcm, IntPtr hwparams); 
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_access")]
        internal static extern int PcmHwParamsSetAccess(IntPtr pcm, IntPtr hwparams, PCMAccess access);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_test_format")]
        internal static extern int PcmHwParamsTestFormat(IntPtr pcm, IntPtr hwparams, PCMFormat format);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_get_format")]
        internal static extern int PcmHwParamsGetFormat(IntPtr pcm, out PCMFormat format);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_format")]
        internal static extern int PcmHwParamsSetFormat(IntPtr pcm, IntPtr hwparams, PCMFormat format);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_rate")]
        internal static extern int PcmHwParamsSetRate(IntPtr pcm, IntPtr hwparams, uint val, int dir);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_channels")]
        internal static extern int PcmHwParamsSetChannels(IntPtr pcm, IntPtr hwparams, uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_periods_near")]
        internal static extern int PcmHwParamsSetPeriodsNear(IntPtr pcm, IntPtr hwparams, ref uint val, ref int dir);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_buffer_size_near")]
        internal static extern int PcmHwParamsSetBufferSizeNear(IntPtr pcm, IntPtr hwparams, ref ulong val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params")]
        internal static extern int PcmHwParams(IntPtr pcm, IntPtr hwparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_malloc")]
        internal static unsafe extern int PcmSwParamsMalloc(out IntPtr swparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_free")]
        internal static unsafe extern void PcmSwParamsFree(IntPtr swparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_current")]
        internal static unsafe extern int PcmSwParamsCurrent(IntPtr pcm, IntPtr swparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params")]
        internal static unsafe extern int PcmSwParams(IntPtr pcm, IntPtr swparams);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_set_avail_min")]
        internal static extern int PcmSwParamsSetAvailMin(IntPtr pcm, IntPtr swparams, ulong val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_writei")]
        internal static unsafe extern int PcmWriteI(IntPtr pcm, byte[] buffer, ulong size);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_readi")]
        internal static unsafe extern int PcmReadI(IntPtr pcm, byte[] buffer, ulong size);
        [DllImport(AlsaLibrary, EntryPoint = "snd_async_add_pcm_handler")]
        internal static extern int AsyncAddPcmHandler(out IntPtr handler, IntPtr pcm, PcmCallback callback, IntPtr private_data);
        [DllImport(AlsaLibrary, EntryPoint = "snd_async_handler_get_pcm")]
        internal static extern IntPtr AsyncHandlerGetPcm(IntPtr handler);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_start")]
        internal static extern int PcmStart(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_drop")]
        internal static extern int PcmDrop(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_drain")]
        internal static extern int PcmDrain(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_pause")]
        internal static extern int PcmPause(IntPtr pcm, int enable);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_avail_update")]
        internal static extern ulong PcmAvailUpdate(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_prepare")]
        internal static extern int PcmPrepare(IntPtr pcm);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_set_start_threshold")]
        internal static extern int PcmSwParamsSetStartThreshold(IntPtr pcm, IntPtr swparams, ulong val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_get_channels_max")]
        internal static extern int PcmHwParamsGetChannelsMax(IntPtr hwparams, out uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_get_channels_min")]
        internal static extern int PcmHwParamsGetChannelsMin(IntPtr hwparams, out uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_test_channels")]
        internal static extern int PcmHwParamsTestChannels(IntPtr pcm, IntPtr hwparams, uint val);
        [DllImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_test_rate")]
        internal static extern int PcmHwParamsTestRate(IntPtr pcm, IntPtr hwparams, uint val, int dir);
        private static string GetString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }
            else
            {
                return Marshal.PtrToStringAnsi(ptr);
            }
        } 
        internal static string CtlCardInfoGetID(IntPtr info)
        {
            return GetString(ctlCardInfoGetID(info));
        }
        internal static string CtlCardInfoGetDriver(IntPtr info)
        {
            return GetString(ctlCardInfoGetDriver(info));
        }
        internal static string CtlCardInfoGetName(IntPtr info)
        {
            return GetString(ctlCardInfoGetName(info));
        }
        internal static string CtlCardInfoGetLongName(IntPtr info)
        {
            return GetString(ctlCardInfoGetLongName(info));
        }
        internal static string CtlCardInfoGetMixerName(IntPtr info)
        {
            return GetString(ctlCardInfoGetMixerName(info));
        }
        internal static string CtlCardInfoGetComponents(IntPtr info)
        {
            return GetString(ctlCardInfoGetComponents(info));
        }
        internal static string PcmInfoGetID(IntPtr pcmInfo)
        {
            return GetString(pcmInfoGetID(pcmInfo));
        }
        internal static string PcmInfoGetName(IntPtr pcmInfo)
        {
            return GetString(pcmInfoGetName(pcmInfo));
        }
        internal static string PcmInfoGetSubdeviceName(IntPtr pcmInfo)
        {
            return GetString(pcmInfoGetSubdeviceName(pcmInfo));
        }
        internal static string ErrorString(int error)
        {
            return GetString(strError(error));
        }
    }
}