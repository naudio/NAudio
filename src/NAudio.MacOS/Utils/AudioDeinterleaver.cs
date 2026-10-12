using System;

namespace NAudio.Utils;

/// <summary>
/// A very simple channel deinterleaver - deinterleaves any samples from a given interleaved buffer.
/// It is used for cases where the HAL is in non-interleaved mode, where each buffer is a single channel of data.
/// </summary>
internal sealed class AudioDeinterleaver
{
    private int blockAlign;
    private AudioBuffer[] channels;

    /// <summary>Creates a new instance of the <see cref="AudioDeinterleaver"/>.</summary>
    public AudioDeinterleaver()
    {
        channels = [];
        blockAlign = 0;
    }

    /// <summary>
    /// Container bytes per sample of the interleaved format.
    /// </summary>
    public int BytesPerFrame
    {
        get => blockAlign;
        set => blockAlign = value;
    }

    /// <summary>
    /// Number of channels.
    /// </summary>
    public int Channels
    {
        get => channels.Length;
        set
        {
            if (value != channels.Length)
            {
                channels = new AudioBuffer[value];
            }
        }
    }

    /// <summary>
    /// Ensures that there can be placed <paramref name="bytesPerChannel"/> bytes in each channel buffer.
    /// </summary>
    /// <param name="bytesPerChannel">The number of bytes to ensure for each channel buffer.</param>
    public void PreEnsureBytes(int bytesPerChannel)
    {
        foreach (var channel in channels)
        {
            channel.EnsureBytes(bytesPerChannel);
        }
    }

    /// <summary>
    /// Deinterleaves the audio data provided in <paramref name="interleavedBuffer"/>.
    /// </summary>
    /// <param name="interleavedBuffer">The buffer to deinterleave and store it's samples to internal buffers</param>
    public void FillFromInterleaved(Span<byte> interleavedBuffer)
    {
        int numberOfChannels = channels.Length;
        int singleChannelBlockAlign = blockAlign / numberOfChannels;
        int samplesPerChannel = interleavedBuffer.Length / blockAlign;

        for (int I = 0; I < numberOfChannels; I++)
        {
            var buffer = channels[I] ??= new();
            buffer.EnsureBytes(samplesPerChannel * singleChannelBlockAlign);
            for (
                int J = 0, sourceIndex = I * singleChannelBlockAlign;
                J < samplesPerChannel;
                J++, sourceIndex += blockAlign
            )
            {
                buffer.PutBytes(interleavedBuffer.Slice(sourceIndex, singleChannelBlockAlign));
            }
        }
    }

    /// <summary>
    /// Gets deinterleaved audio samples for the specified channel.
    /// </summary>
    /// <param name="channel">The channel to get samples for</param>
    /// <param name="target">The buffer to store the samples to</param>
    /// <returns># of bytes provided into the <paramref name="target"/> span.</returns>
    public int GetChannelBytes(int channel, Span<byte> target) => channels[channel].GetBytes(target);

    /// <summary>Clears any data for the given channel.</summary>
    /// <remarks>
    /// This method can be used when a particular channel is not used.
    /// </remarks>
    /// <param name="channel">The channel to clear.</param>
    public void ClearChannelBuffer(int channel) => channels[channel].Clear();
}
