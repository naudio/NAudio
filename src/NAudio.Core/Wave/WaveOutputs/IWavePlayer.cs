
using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Plays the audio from the <see cref="IWaveProvider"/> passed to its <see cref="Init" /> method on an output device. <br />
/// This interface is implemented by NAudio's audio output classes, which wrap a platform audio API such as WASAPI on Windows. <br />
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

    /// <summary>Pause playback. Call <see cref="Play"/> to resume.</summary>
    void Pause();

    /// <summary>
    /// Initialises playback from a given wave
    /// provider that provides the samples to play.
    /// </summary>
    /// <param name="waveProvider">The wave provider to be played.</param>
    void Init(IWaveProvider waveProvider);

    /// <summary>
    /// Allows to modify the gain (volume) of the output audio. <br />
    /// 1.0f is full scale, while 0.0f is silence.
    /// </summary>
    /// <value>
    /// A value indicating the gain (volume) of the output audio. <br />
    /// Implementations are permitted to always return 1f if the driver does
    /// not have gain controls.
    /// </value>
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
    /// <value>
    /// A value from the <see cref="Wave.PlaybackState"/> 
    /// enumeration that indicates the state of the playback.
    /// </value>
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
    /// Event that is raised when playback has stopped. <br />
    /// Playback can be stopped due to any of the following three reasons: <br />
    /// <list type="bullet">
    ///     <item>
    ///         You called the <see cref="Stop"/> method. <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <see langword="null" />.
    ///     </item>
    ///     <item>
    ///         The provided <see cref="IWaveProvider"/> instance
    ///         has reached its end (no more samples to play). <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <see langword="null" />.
    ///     </item>
    ///     <item>
    ///         An error/exception occurred either by calling <see cref="IWaveProvider.Read(Span{byte})" />,
    ///         or the driver has encountered a failure which it forced to stop playback. <br />
    ///         In this case, the event args object 
    ///         <see cref="StoppedEventArgs.Exception" /> property is <em>not</em> <see langword="null" />.
    ///     </item>
    /// </list>
    /// </summary>
    event EventHandler<StoppedEventArgs> PlaybackStopped;
}
