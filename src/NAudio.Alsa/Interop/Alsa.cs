using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// Source-generated P/Invoke surface for the ALSA <c>libasound</c> library.
    /// </summary>
    /// <remarks>
    /// The runtime SONAME on Linux is <c>libasound.so.2</c>; the bare
    /// <c>libasound.so</c> symlink only exists when the <c>-dev</c> package is
    /// installed. A <see cref="NativeLibrary"/> resolver maps the import name
    /// to the versioned SONAME so the library loads on end-user machines that
    /// only have the runtime package.
    /// </remarks>
    internal static partial class AlsaInterop
    {
        private const string AlsaLibrary = "libasound";
        private const string LibC = "libc";

        [ModuleInitializer]
        internal static void RegisterResolver()
        {
            NativeLibrary.SetDllImportResolver(typeof(AlsaInterop).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            string[] candidates = libraryName switch
            {
                AlsaLibrary => new[] { "libasound.so.2", "libasound.so", "libasound" },
                LibC => new[] { "libc.so.6", "libc.so", "libc" },
                _ => null,
            };

            if (candidates == null)
            {
                return IntPtr.Zero;
            }

            foreach (var candidate in candidates)
            {
                if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }

        // --- Device-name hints (enumeration) -----------------------------

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_device_name_hint", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int DeviceNameHint(int card, string iface, out IntPtr hints);

        // Returns a strdup'd ASCII string the caller must release with libc free().
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_device_name_get_hint", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr DeviceNameGetHint(IntPtr hint, string id);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_device_name_free_hint")]
        internal static partial int DeviceNameFreeHint(IntPtr hints);

        [LibraryImport(LibC, EntryPoint = "free")]
        internal static partial void Free(IntPtr ptr);

        // --- PCM lifecycle ------------------------------------------------

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_open", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int PcmOpen(out IntPtr pcm, string name, PCMStream stream, int mode);

        // Raw close used only by SafePcmHandle.ReleaseHandle; everywhere else
        // the handle's lifetime is owned by the SafeHandle.
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_close")]
        internal static partial int PcmClose(IntPtr pcm);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_prepare")]
        internal static partial int PcmPrepare(IntPtr pcm);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_start")]
        internal static partial int PcmStart(IntPtr pcm);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_drop")]
        internal static partial int PcmDrop(IntPtr pcm);

        // Blocking (in blocking mode): plays out queued frames before
        // returning. Used at natural end-of-stream so the tail isn't lost.
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_drain")]
        internal static partial int PcmDrain(IntPtr pcm);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_pause")]
        internal static partial int PcmPause(IntPtr pcm, int enable);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_recover")]
        internal static partial int PcmRecover(IntPtr pcm, int err, int silent);

        // snd_pcm_avail_update / writei / readi return snd_pcm_sframes_t
        // (ssize_t, pointer-sized signed). Declaring them as int/ulong (as the
        // PoC did) loses the sign on xrun and truncates on 64-bit.
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_writei")]
        internal static partial nint PcmWriteI(IntPtr pcm, ReadOnlySpan<byte> buffer, nuint frames);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_readi")]
        internal static partial nint PcmReadI(IntPtr pcm, Span<byte> buffer, nuint frames);

        // --- Hardware parameters -----------------------------------------

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_malloc")]
        internal static partial int PcmHwParamsMalloc(out IntPtr hwparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_free")]
        internal static partial void PcmHwParamsFree(IntPtr hwparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_any")]
        internal static partial int PcmHwParamsAny(IntPtr pcm, IntPtr hwparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params")]
        internal static partial int PcmHwParams(IntPtr pcm, IntPtr hwparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_access")]
        internal static partial int PcmHwParamsSetAccess(IntPtr pcm, IntPtr hwparams, PCMAccess access);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_test_format")]
        internal static partial int PcmHwParamsTestFormat(IntPtr pcm, IntPtr hwparams, PCMFormat format);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_format")]
        internal static partial int PcmHwParamsSetFormat(IntPtr pcm, IntPtr hwparams, PCMFormat format);

        // _near (not the exact _set_rate): real hw: devices reject a rate
        // they cannot deliver exactly. snd_pcm_hw_params_set_rate_near
        // picks the closest supported rate (in/out via ref).
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_rate_near")]
        internal static partial int PcmHwParamsSetRateNear(IntPtr pcm, IntPtr hwparams, ref uint val, ref int dir);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_channels")]
        internal static partial int PcmHwParamsSetChannels(IntPtr pcm, IntPtr hwparams, uint val);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_periods_near")]
        internal static partial int PcmHwParamsSetPeriodsNear(IntPtr pcm, IntPtr hwparams, ref uint val, ref int dir);

        // snd_pcm_uframes_t* -> ref nuint (the PoC used ref ulong, wrong on 32-bit).
        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_hw_params_set_buffer_size_near")]
        internal static partial int PcmHwParamsSetBufferSizeNear(IntPtr pcm, IntPtr hwparams, ref nuint val);

        // --- Software parameters -----------------------------------------

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_malloc")]
        internal static partial int PcmSwParamsMalloc(out IntPtr swparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_free")]
        internal static partial void PcmSwParamsFree(IntPtr swparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_current")]
        internal static partial int PcmSwParamsCurrent(IntPtr pcm, IntPtr swparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params")]
        internal static partial int PcmSwParams(IntPtr pcm, IntPtr swparams);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_set_avail_min")]
        internal static partial int PcmSwParamsSetAvailMin(IntPtr pcm, IntPtr swparams, nuint val);

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_pcm_sw_params_set_start_threshold")]
        internal static partial int PcmSwParamsSetStartThreshold(IntPtr pcm, IntPtr swparams, nuint val);

        // --- Errors -------------------------------------------------------

        [LibraryImport(AlsaLibrary, EntryPoint = "snd_strerror")]
        private static partial IntPtr StrError(int error);

        // --- Managed helpers for library-owned const char* returns --------

        private static string GetString(IntPtr ptr)
            => ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;

        internal static string ErrorString(int error) => GetString(StrError(error));

        /// <summary>
        /// Reads a hint field (<c>NAME</c>/<c>DESC</c>/<c>IOID</c>) and frees
        /// the strdup'd result with libc <c>free</c>. Returns <c>null</c> when
        /// the field is absent (notably <c>IOID</c>, meaning bidirectional).
        /// </summary>
        internal static string DeviceNameGetHintString(IntPtr hint, string id)
        {
            var ptr = DeviceNameGetHint(hint, id);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUTF8(ptr);
            }
            finally
            {
                Free(ptr);
            }
        }

    }
}
