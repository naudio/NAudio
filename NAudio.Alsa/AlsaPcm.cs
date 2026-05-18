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
    /// <remarks>
    /// Threading model: control-plane calls (open/configure/prepare/pause/
    /// drop/stop/dispose) are serialised by <c>gate</c> and refuse to touch
    /// the handle once disposed. The data plane (the streaming worker's
    /// blocking <c>writei</c>/<c>readi</c>) runs without the lock and is made
    /// safe by stopping + joining the worker before the handle is closed.
    /// <see cref="StopWorker"/> is idempotent and safe to call from the
    /// worker itself (e.g. <c>Stop()</c> from a <c>DataAvailable</c> handler):
    /// it never self-joins. <see cref="Dispose"/> never holds <c>gate</c>
    /// across the join, so a handler that calls back in cannot deadlock.
    /// </remarks>
    internal sealed class AlsaPcm : IDisposable
    {
        /// <summary>Frames per period (one transfer chunk).</summary>
        internal const int PeriodFrames = 1024;

        /// <summary>Number of periods in the device ring buffer.</summary>
        internal const uint Periods = 4;

        private readonly object gate = new();
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

        /// <summary>
        /// Raw <c>snd_pcm_t*</c> for the P/Invoke layer. Only safe on the
        /// streaming worker thread (the handle is not closed until the worker
        /// has been joined).
        /// </summary>
        internal IntPtr Pcm => handle.DangerousGetHandle();

        /// <summary>True while the streaming worker should keep running.</summary>
        internal bool Running => running;

        /// <summary>Throws if this device has already been disposed.</summary>
        internal void ThrowIfDisposed()
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
            }
        }

        /// <summary>True if the device supports the given interleaved format.</summary>
        internal bool SupportsFormat(PCMFormat format)
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
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
        }

        /// <summary>Negotiates the interleaved hardware parameters for the device.</summary>
        internal void ConfigureHardware(PCMFormat format, int channels, int sampleRate)
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
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

                    uint rate = (uint)sampleRate;
                    int dir = 0;
                    AlsaException.ThrowIfError(
                        AlsaInterop.PcmHwParamsSetRateNear(Pcm, hw, ref rate, ref dir),
                        "snd_pcm_hw_params_set_rate_near");

                    uint periods = Periods;
                    dir = 0;
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
        }

        /// <summary>Sets the software wake-up granularity and start threshold.</summary>
        internal void ConfigureSoftware(uint startThresholdFrames)
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
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
        }

        /// <summary>Prepares the device for use after configuration or an xrun.</summary>
        internal void Prepare()
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                AlsaException.ThrowIfError(AlsaInterop.PcmPrepare(Pcm), "snd_pcm_prepare");
            }
        }

        /// <summary>
        /// Attempts a hardware pause/resume. Returns the libasound result
        /// (negative if the driver cannot pause, or if disposed).
        /// </summary>
        internal int Pause(int enable)
        {
            lock (gate)
            {
                return disposed ? -1 : AlsaInterop.PcmPause(Pcm, enable);
            }
        }

        /// <summary>Discards buffered frames (no-op once disposed).</summary>
        internal void Drop()
        {
            lock (gate)
            {
                if (!disposed && !handle.IsInvalid)
                {
                    AlsaInterop.PcmDrop(Pcm);
                }
            }
        }

        /// <summary>Starts the background streaming thread running <paramref name="loop"/>.</summary>
        internal void StartWorker(string name, Action loop)
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                running = true;
                worker = new Thread(() => loop())
                {
                    Name = name,
                    IsBackground = true,
                    // Matches NAudio's WinMM WaveOut/WaveIn. On Linux this is
                    // a nice() bump (no root needed); glitch-free audio under
                    // heavy load still wants RT scheduling (rtkit/SCHED_FIFO),
                    // which is the caller's/system's concern.
                    Priority = ThreadPriority.AboveNormal,
                };
                worker.Start();
            }
        }

        /// <summary>
        /// Signals the worker to stop and unblocks any in-flight blocking I/O
        /// via <c>snd_pcm_drop</c>, then joins it. Idempotent. If called from
        /// the worker thread itself (e.g. <c>Stop()</c> inside a
        /// <c>DataAvailable</c> handler) it does not join — the loop observes
        /// <see cref="Running"/> == false and exits on its own.
        /// </summary>
        internal void StopWorker()
        {
            Thread toJoin;
            lock (gate)
            {
                toJoin = worker;
                if (toJoin == null)
                {
                    return;
                }

                running = false;
                if (!disposed && !handle.IsInvalid)
                {
                    AlsaInterop.PcmDrop(Pcm);
                }

                if (toJoin == Thread.CurrentThread)
                {
                    return;
                }
            }

            toJoin.Join();
            lock (gate)
            {
                if (worker == toJoin)
                {
                    worker = null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Thread toJoin;
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                running = false;
                toJoin = worker;
                if (toJoin != null && !handle.IsInvalid)
                {
                    AlsaInterop.PcmDrop(Pcm);
                }
            }

            // Joined outside the lock so a PlaybackStopped/RecordingStopped
            // handler that calls back in (Stop/Dispose) cannot deadlock.
            if (toJoin != null && toJoin != Thread.CurrentThread)
            {
                toJoin.Join();
            }

            lock (gate)
            {
                worker = null;
            }

            handle.Dispose();
        }
    }
}
