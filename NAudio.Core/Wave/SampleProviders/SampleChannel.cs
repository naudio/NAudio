using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Utility class that takes an IAudioSource input at any bit depth
    /// and exposes it as an ISampleSource. Can turn mono inputs into stereo,
    /// and allows adjusting of volume.
    /// (The eventual successor to WaveChannel32)
    /// This class also serves as an example of how you can link together several simple
    /// sample sources to form a more useful class.
    /// </summary>
    public class SampleChannel : ISampleSource
    {
        private readonly VolumeSampleProvider volumeProvider;
        private readonly MeteringSampleProvider preVolumeMeter;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Initialises a new instance of SampleChannel
        /// </summary>
        /// <param name="audioSource">Source audio, must be PCM or IEEE</param>
        public SampleChannel(IAudioSource audioSource)
            : this(audioSource, false)
        {
        }

        /// <summary>
        /// Initialises a new instance of SampleChannel
        /// </summary>
        /// <param name="audioSource">Source audio, must be PCM or IEEE</param>
        /// <param name="forceStereo">force mono inputs to become stereo</param>
        public SampleChannel(IAudioSource audioSource, bool forceStereo)
        {
            ISampleSource sampleSource = SampleProviderConverters
                .ConvertAudioSourceIntoSampleSource(audioSource);
            if (sampleSource.WaveFormat.Channels == 1 && forceStereo)
            {
                sampleSource = new MonoToStereoSampleProvider(sampleSource);
            }
            waveFormat = sampleSource.WaveFormat;
            // let's put the meter before the volume (useful for drawing waveforms)
            preVolumeMeter = new MeteringSampleProvider(sampleSource);
            volumeProvider = new VolumeSampleProvider(preVolumeMeter);
        }

        /// <summary>
        /// Reads samples from this sample source
        /// </summary>
        public int Read(Span<float> buffer)
        {
            return volumeProvider.Read(buffer);
        }

        /// <summary>
        /// The WaveFormat of this Sample Source
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float Volume
        {
            get { return volumeProvider.Volume; }
            set { volumeProvider.Volume = value; }
        }

        /// <summary>
        /// Raised periodically to inform the user of the max volume
        /// (before the volume meter)
        /// </summary>
        public event EventHandler<StreamVolumeEventArgs> PreVolumeMeter
        {
            add { preVolumeMeter.StreamVolume += value; }
            remove { preVolumeMeter.StreamVolume -= value; }
        }
    }
}
