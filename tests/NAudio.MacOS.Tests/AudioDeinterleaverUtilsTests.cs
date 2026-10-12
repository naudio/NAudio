
using System;
using System.Runtime.InteropServices;

using NAudio.Utils;
using NAudio.Wave.SampleProviders;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

[TestFixture]
public class AudioDeinterleaverUtilsTests
{
    // Creates sine wave data that are split in buffers per-channel, as macOS Audio HAL
    // provides the buffers in capture mode (with the non-interleaved flag).
    // It also provides the original sample buffer, so that the tests can compare it against the original buffer.
    private static (float[][] deinterleaved, float[] original) CreateSineWave(int sampleRate, int channels, double durationSeconds, double frequency)
    {
        var signal = new SignalGenerator(sampleRate, channels)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = frequency,
            Gain = 0.8
        };

        var sampleCount = (int)(sampleRate * channels * durationSeconds);
        var sampleBuffer = new float[sampleCount];
        var read = signal.Read(sampleBuffer.AsSpan());

        Assert.That(read, Is.EqualTo(sampleBuffer.Length));

        int samplesPerChannel = sampleCount / channels;

        float[][] buffers = new float[channels][];

        for (int I = 0; I < channels; I++)
        {
            float[] buffer = buffers[I] = new float[samplesPerChannel];
            // Deinterleave the samples.
            for (int J = 0, sourceBufferIndex = I; J < samplesPerChannel; J++, sourceBufferIndex += channels)
            {
                buffer[J] = sampleBuffer[sourceBufferIndex];
            }
        }

        return (buffers, sampleBuffer);
    }

    [TestCase(4)]
    [TestCase(8)]
    [TestCase(12)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(7)]
    public void TestAudioDeinterleaver(int nChannels)
    {
        float[][] deinterleaved;
        float[] originalInterleaved;

        (deinterleaved, originalInterleaved) = CreateSineWave(48000, nChannels, 1d, 440d);

        // Pass the originalInterleaved buffer to the deinterleaver.

        AudioDeinterleaver deinterleaver = new();

        deinterleaver.Channels = nChannels;
        deinterleaver.BytesPerFrame = sizeof(float) * nChannels;

        deinterleaver.FillFromInterleaved(MemoryMarshal.Cast<float, byte>(originalInterleaved.AsSpan()));

        for (int I = 0; I < nChannels; I++)
        {
            float[] retrievedBuffer = new float[deinterleaved[I].Length];
            int read = deinterleaver.GetChannelBytes(I, MemoryMarshal.Cast<float, byte>(retrievedBuffer.AsSpan()));
            Assert.That(read, Is.EqualTo(retrievedBuffer.Length * sizeof(float)), "Number of read bytes must be equal to the size of the buffer.");
            Assert.That(retrievedBuffer.AsSpan().SequenceEqual(deinterleaved[I]), "The produced deinterleaved buffer contents are not the expected.");
        }
    }

    // Ensures that the PreEnsureBytes method succeeds.
    // Previously, the method failed with NullReferenceException, which caused the player (which uses this) to fail.
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(10)]
    public void VerifyThatPreEnsureBytesDoesNotFail(int nChannels)
    {
        AudioDeinterleaver deinterleaver = new();

        deinterleaver.Channels = nChannels;
        deinterleaver.BytesPerFrame = sizeof(float);

        // Ensure 100 bytes for each buffer.
        // This allocation should typically succeed on test machines,
        // as it is trivial.
        Assert.DoesNotThrow(() => deinterleaver.PreEnsureBytes(100));
    }

    /**
     * To assert that the deinterleaver can correctly handle cases under streaming (which is the typical use case),
     * we will use a random source to roll the number of bytes to copy for get operations.
     * If, finally, the buffers do match, the test is successfull. The test will be repeated a couple of times
     * to ensure that we test the deinterleaver in different cases.
     */

    [Test]
    [Repeat(5)]
    public void TestAudioDeinterleaver_Streaming()
    {
        Random randomSource = new();

        float[][] deinterleaved;
        float[] originalInterleaved;

        (deinterleaved, originalInterleaved) = CreateSineWave(48000, 4, 1d, 440d);

        int totalBytes = originalInterleaved.Length * sizeof(float);

        // The buffer indexes array is holding the index where the byte
        // representation of each decoded buffer should write data to.
        int[] bufferIndexes = new int[deinterleaved.Length];
        float[][] decodedBuffers = new float[deinterleaved.Length][];

        AudioDeinterleaver deinterleaver = new();

        deinterleaver.Channels = deinterleaved.Length;
        deinterleaver.BytesPerFrame = sizeof(float) * deinterleaved.Length;

        deinterleaver.FillFromInterleaved(MemoryMarshal.Cast<float, byte>(originalInterleaved.AsSpan()));

        while (totalBytes > 0)
        {
            for (int channel = 0; channel < deinterleaved.Length; channel++)
            {
                Span<byte> byteSpan = MemoryMarshal.Cast<float, byte>(
                    (decodedBuffers[channel] ??= new float[deinterleaved[channel].Length]).AsSpan()
                );
                int read = deinterleaver.GetChannelBytes(
                    channel,
                    byteSpan.Slice(
                        bufferIndexes[channel],
                        Math.Min(byteSpan.Length - bufferIndexes[channel], randomSource.Next(1, 100) * sizeof(float))
                    )
                );
                bufferIndexes[channel] += read;
                totalBytes -= read;
            }
        }

        // Now, check the buffers.
        for (int channel = 0; channel < deinterleaved.Length; channel++)
        {
            Assert.That(deinterleaved[channel].AsSpan().SequenceEqual(decodedBuffers[channel]), "The produced deinterleaved buffer contents are not the expected.");
        }
    }

}
