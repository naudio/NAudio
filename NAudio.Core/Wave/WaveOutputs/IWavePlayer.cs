using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Represents the interface to a device that can play a WaveFile
    /// </summary>
    public interface IWavePlayer : IDisposable
    {
        /// <summary>
        /// Begin playback
        /// </summary>
        void Play();

        /// <summary>
        /// Stop playback
        /// </summary>
        void Stop();

        /// <summary>
        /// Pause Playback
        /// </summary>
        void Pause();

        /// <summary>
        /// Initialise playback
        /// </summary>
        /// <param name="waveProvider">The wave provider to be played</param>
        void Init(IWaveProvider waveProvider);

        /// <summary>
        /// The volume 
        /// 1.0f is full scale
        /// Note that not all implementations necessarily support volume changes
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Current playback state
        /// </summary>
        PlaybackState PlaybackState { get; }

        /// <summary>
        /// Indicates that playback has gone into a stopped state due to 
        /// reaching the end of the input stream or an error has been encountered during playback
        /// </summary>
        event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// The WaveFormat this device is using for playback
        /// </summary>
        WaveFormat OutputWaveFormat { get; }
    }

    /// <summary>
    /// Interface for IWavePlayers that can report position
    /// </summary>
    public interface IWavePosition
    {
        /// <summary>
        /// Position (in terms of bytes played - does not necessarily translate directly to the position within the source audio file)
        /// </summary>
        /// <returns>Position in bytes</returns>
        long GetPosition();

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        WaveFormat OutputWaveFormat { get; }
    }

    /// <summary>
    /// Interface for wave players (and, in principle, wave inputs) that can report the latency
    /// between a sample entering the output pipeline and emerging from the audio hardware.
    /// Implement this alongside <see cref="IWavePlayer"/> to let downstream code synchronise
    /// visualisations, lighting, video, or any other time-aligned consumer with audible output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Latency values are only meaningful during uninterrupted playback. When the device is
    /// stopped or paused, or when an underrun has occurred, implementations should return their
    /// best steady-state estimate rather than throw — consumers driving visualisations need a
    /// stable value to fall back on.
    /// </para>
    /// <para>
    /// The two properties answer different questions. <see cref="AverageLatency"/> describes the
    /// pipeline (it changes only when buffer settings change), so it is the right value for
    /// scheduling. <see cref="CurrentLatency"/> describes the live state of the pipeline at this
    /// instant and is the right value for drift detection or per-frame correction.
    /// </para>
    /// <para>
    /// For drivers whose buffer scheduling is fully predictable (notably ASIO, which always swaps
    /// fixed-size buffers at regular intervals), <see cref="CurrentLatency"/> may simply return
    /// <see cref="AverageLatency"/>. The approximation is exact to within half a buffer.
    /// </para>
    /// </remarks>
    public interface IWaveLatency
    {
        /// <summary>
        /// The steady-state latency from a sample being queued for output to it being emitted by
        /// the audio hardware, assuming uninterrupted playback. This is a property of the buffer
        /// configuration and the driver, not of the current playback state.
        /// </summary>
        TimeSpan AverageLatency { get; }

        /// <summary>
        /// The time that has elapsed since the sample currently emerging from the hardware was
        /// queued for output. In steady state this is approximately equal to
        /// <see cref="AverageLatency"/>; it differs during start-up, after an underrun, or when
        /// the host is filling buffers irregularly. Implementations that cannot meaningfully
        /// distinguish from the average are permitted to return <see cref="AverageLatency"/>.
        /// </summary>
        TimeSpan CurrentLatency { get; }
    }
}
