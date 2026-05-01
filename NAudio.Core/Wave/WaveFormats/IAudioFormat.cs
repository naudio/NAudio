using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Basic abstraction of a wave format instance - abstracts away the format details on each platform. <br />
    /// Experimental - Will likely change in the future.
    /// </summary>
    public interface IAudioFormat
    {
        /// <summary>
        /// Gets the number of samples that each second of the audio data contain.
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Gets the bit-rate of the audio data. <br />
        /// Specifically, each sample is consisted of bits returned by this property.
        /// </summary>
        int BitsPerSample { get; }

        /// <summary>
        /// Specifies how the channels are arranged in a audio stream. <br />
        /// The Length property of the returned array specifies how many 
        /// channels are existing on the audio stream.
        /// </summary>
        ChannelType[] ChannelLayout { get; }

        /// <summary>
        /// Gets the exact size, in bytes, of each sample. <br />
        /// There might be cases that the sample might not be a multiple of 8 bits, 
        /// which in such case the sample size is extended to the closest power of two.
        /// </summary>
        /// <remarks>The default implementation of the getter is <c><see cref="BitsPerSample"/> * 8</c>.</remarks>
        virtual int BlockAlign => BitsPerSample * 8;

        /// <summary>
        /// Gets an estimate of how many bytes are required to store a single second of audio data.
        /// </summary>
        /// <remarks>The default implementation of the getter is <c><see cref="SampleRate"/> * <see cref="BlockAlign"/></c>.</remarks>
        virtual int AverageBytesPerSecond => SampleRate * BlockAlign;
    }
}
