using System;

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
        /// Resume Playback following pause
        /// </summary>
        void Resume();

        /// <summary>
        /// Initialise playback
        /// </summary>
        /// <param name="waveStream">The wavestream to be played</param>
        void Init(WaveStream waveStream);

        /// <summary>
        /// Indicates whether device is playing
        /// n.b. With some combinations of waveStream input, IsPlaying
        /// will continue to return true even after 
        /// waveStream.Position > waveStream.Length
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Indicates whether device is paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// The volume. 1.0 is full scale
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Pan / Balance from -1.0 to 1.0
        /// </summary>
        float Pan { get; set; }

    }
}
