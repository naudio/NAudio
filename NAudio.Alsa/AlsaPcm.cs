using System;
using System.Threading;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// Encapsulates an open ALSA PCM device: handle ownership, interleaved
    /// parameter negotiation and the background streaming thread. Used by
    /// composition from <see cref="AlsaOut"/> / <see cref="AlsaIn"/> so none
    /// of the native surface leaks onto the public API.
    /// </summary>
    internal sealed class AlsaPcm : IDisposable
    {
        /// <summary>Frames per period (one transfer chunk).</summary>
        internal const int PeriodFrames = 1024;

        /// <summary>Number of periods in the device ring buffer.</summary>
        internal const uint Periods = 4;

        private readonly SafePcmHandle handle;
        private bool disposed;
        private Thread worker;
        private volatile bool running;

        internal AlsaPcm(string pcmName, PCMStream stream)
        {
            AlsaException.ThrowIfError(
                AlsaInterop.PcmOpen(out var raw, pcmName, stream, 0),
                "snd_pcm_open");
            handle = new SafePcmHandle(raw);
        }

        /// <summary>Raw <c>snd_pcm_t*</c> for the P/Invoke layer.</summary>
        internal IntPtr Pcm => handle.DangerousGetHandle();

        /// <summary>True while the streaming worker should keep running.</summary>
        internal bool Running => running;

        /// <summary>Throws if this device has already been disposed.</summary>
        internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

        /// <summary>True if the device supports the given interleaved format.</summary>
        internal bool SupportsFormat(PCMFormat format)
        {
            AlsaException.ThrowIfError(AlsaInterop.PcmHwParamsMalloc(out var hw), "snd_pcm_hw_params_malloc");
            try
            {
                AlsaException.ThrowIfError(AlsaInterop.PcmHwParamsAny(Pcm, hw), "snd_pcm_hw_params_any");
                return AlsaInterop.PcmHwParamsTestFormat(Pcm, hw, format) == 0;
            }
            finally
            {
                AlsaInterop.PcmHwParamsFree(hw);
            }
        }

        /// <summary>Negotiates the interleaved hardware parameters for the device.</summary>
        internal void ConfigureHardware(PCMFormat format, int channels, int sampleRate)
        {
            AlsaException.ThrowIfError(AlsaInterop.PcmHwParamsMalloc(out var hw), "snd_pcm_hw_params_malloc");
            try
            {
                AlsaException.ThrowIfError(AlsaInterop.PcmHwParamsAny(Pcm, hw), "snd_pcm_hw_params_any");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetAccess(Pcm, hw, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED),
                    "snd_pcm_hw_params_set_access");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetFormat(Pcm, hw, format), "snd_pcm_hw_params_set_format");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetChannels(Pcm, hw, (uint)channels), "snd_pcm_hw_params_set_channels");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetRate(Pcm, hw, (uint)sampleRate, 0), "snd_pcm_hw_params_set_rate");

                uint periods = Periods;
                int dir = 0;
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetPeriodsNear(Pcm, hw, ref periods, ref dir),
                    "snd_pcm_hw_params_set_periods_near");

                nuint bufferFrames = (nuint)(PeriodFrames * Periods);
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmHwParamsSetBufferSizeNear(Pcm, hw, ref bufferFrames),
                    "snd_pcm_hw_params_set_buffer_size_near");

                AlsaException.ThrowIfError(AlsaInterop.PcmHwParams(Pcm, hw), "snd_pcm_hw_params");
            }
            finally
            {
                AlsaInterop.PcmHwParamsFree(hw);
            }
        }

        /// <summary>Sets the software wake-up granularity and start threshold.</summary>
        internal void ConfigureSoftware(uint startThresholdFrames)
        {
            AlsaException.ThrowIfError(AlsaInterop.PcmSwParamsMalloc(out var sw), "snd_pcm_sw_params_malloc");
            try
            {
                AlsaException.ThrowIfError(AlsaInterop.PcmSwParamsCurrent(Pcm, sw), "snd_pcm_sw_params_current");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmSwParamsSetAvailMin(Pcm, sw, PeriodFrames), "snd_pcm_sw_params_set_avail_min");
                AlsaException.ThrowIfError(
                    AlsaInterop.PcmSwParamsSetStartThreshold(Pcm, sw, startThresholdFrames),
                    "snd_pcm_sw_params_set_start_threshold");
                AlsaException.ThrowIfError(AlsaInterop.PcmSwParams(Pcm, sw), "snd_pcm_sw_params");
            }
            finally
            {
                AlsaInterop.PcmSwParamsFree(sw);
            }
        }

        /// <summary>Prepares the device for use after configuration or an xrun.</summary>
        internal void Prepare()
            => AlsaException.ThrowIfError(AlsaInterop.PcmPrepare(Pcm), "snd_pcm_prepare");

        /// <summary>Starts the background streaming thread running <paramref name="loop"/>.</summary>
        internal void StartWorker(string name, Action loop)
        {
            running = true;
            worker = new Thread(() => loop()) { Name = name, IsBackground = true };
            worker.Start();
        }

        /// <summary>
        /// Signals the worker to stop, unblocks any in-flight blocking I/O via
        /// <c>snd_pcm_drop</c>, and joins the thread so no P/Invoke is in
        /// flight when the handle is closed.
        /// </summary>
        internal void StopWorker()
        {
            if (worker == null)
            {
                return;
            }

            running = false;
            if (!handle.IsInvalid)
            {
                AlsaInterop.PcmDrop(Pcm);
            }

            worker.Join();
            worker = null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            StopWorker();
            handle.Dispose();
        }
    }
}
