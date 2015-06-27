using System;
using System.Linq;
using System.Threading.Tasks;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents the interface to a device that can play audio
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
        /// Obsolete init method
        /// </summary>
        /// <param name="waveProvider"></param>
        /// <returns></returns>
        [Obsolete]
        Task Init(IWaveProvider waveProvider);

        /// <summary>
        /// Initialise playback
        /// </summary>
        /// <param name="waveProviderFunc">Function to create the waveprovider to be played
        /// Called on the playback thread</param>
        void Init(Func<IWaveProvider> waveProviderFunc);

        /// <summary>
        /// Current playback state
        /// </summary>
        PlaybackState PlaybackState { get; }

        /// <summary>
        /// Indicates that playback has gone into a stopped state due to 
        /// reaching the end of the input stream or an error has been encountered during playback
        /// </summary>
        event EventHandler<StoppedEventArgs> PlaybackStopped;
    }
}
