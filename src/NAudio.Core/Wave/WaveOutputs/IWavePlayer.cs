
using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Represents a hardware audio device that can play any <see cref="IWaveProvider"/> 
/// instance provided to it's <see cref="Init" /> method. <br />
/// This interface is typically implemented by audio drivers, such as WASAPI on Windows. <br />
/// You are expected to release the acquired audio hardware 
/// (by calling <see cref="IDisposable.Dispose" />) once you are finished using it.
/// </summary>
/// <seealso cref="IWavePosition" />
/// <seealso cref="IWaveLatency" />
public interface IWavePlayer : IDisposable
{
    /// <summary>Begin playback</summary>
    void Play();

    /// <summary>Stop playback</summary>
    void Stop();

    /// <summary>Pause Playback</summary>
    /// <remarks>
    /// The way this method works is that during the Pause state,
    /// the buffers of the driver are not yet cleared, but the
    /// playback has been stopped. As such, later resuming playback
    /// is faster than doing <see cref="Stop"/>, then <see cref="Play"/>. <br />
    /// Note that not all drivers do necessarily implement this
    /// because it depends on the programming model the driver is using.
    /// </remarks>
    void Pause();

    /// <summary>
    /// Initialises playback from a given wave
    /// provider that provides the samples to play.
    /// </summary>
    /// <param name="waveProvider">The wave provider to be played.</param>
    /// <exception cref="ArgumentNullException"><paramref name="waveProvider" /> is <see langword="null" />.</exception>
    void Init(IWaveProvider waveProvider);

    /// <summary>
    /// Allows to modify the gain (volume) of the output audio. <br />
    /// 1.0f is full scale, while 0.0f is silence.
    /// </summary>
    /// <returns>
    /// A value indicating the gain (volume) of the output audio. <br />
    /// Implementations are permitted to always return 1f if the driver does
    /// not have gain controls.
    /// </returns>
    /// <remarks>
    /// Not all implementations do necessarily support getting/setting a volume value. <br />
    /// In some drivers, this modifies the stream's volume, and in some others the global or the device's hardware volume.
    /// </remarks>
    /// <exception cref="InvalidOperationException">When setting the property: Volume changes are not supported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When setting the property: The new volume exceeds 1f, or is less than 0f.</exception>
    float Volume { get; set; }

    /// <summary>
    /// Queries the current state of the playback.
    /// </summary>
    /// <returns>
    /// A value from the <see cref="Wave.PlaybackState"/> 
    /// enumeration that indicates the state of the playback.
    /// </returns>
    PlaybackState PlaybackState { get; }

    /// <summary>
    /// The audio format under which the driver
    /// provides samples to the connected-to hardware device.
    /// </summary>
    /// <remarks>
    /// This is not necessarily the same format as the 
    /// one provided from the <see cref="IWaveProvider" /> during <see cref="Init" />.
    /// </remarks>
    WaveFormat OutputWaveFormat { get; }

    /// <summary>
    /// Event that is dispatching when playback has been stopped. <br />
    /// Playback can be stopped due to any of the following three reasons: <br />
    /// <list type="bullet">
    ///     <item>
    ///         You called the <see cref="Stop"/> method. <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <see langword="null" />.
    ///     </item>
    ///     <item>
    ///         The provided <see cref="IWaveProvider"/> instance
    ///         has reached it's end (no more samples to play). <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <see langword="null" />.
    ///     </item>
    ///     <item>
    ///         An error/exception has been occurred either by calling <see cref="IWaveProvider.Read(Span{byte})" />,
    ///         or the driver has encountered a failure which it forced to stop playback. <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <em>not</em> <see langword="null" />.
    ///     </item>
    /// </list>
    /// </summary>
    event EventHandler<StoppedEventArgs> PlaybackStopped;
}
