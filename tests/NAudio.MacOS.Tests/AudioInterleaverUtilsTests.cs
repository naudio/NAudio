
using System;
using System.Runtime.InteropServices;

using NUnit.Framework;

using NAudio.Utils;
using NAudio.Wave.SampleProviders;

namespace NAudio.MacOS.Tests;

[TestFixture]
public sealed class AudioInterleaverUtilsTests
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
    public void TestAudioInterleaver(int nChannels)
    {
        float[][] deinterleaved;
        float[] originalInterleaved;

        (deinterleaved, originalInterleaved) = CreateSineWave(48000, nChannels, 1d, 440d);

        AudioInterleaver interleaver = new();

        interleaver.BytesPerFrame = sizeof(float); // 32-bit IEEE floating-point
        interleaver.Channels = nChannels;

        interleaver.PrepareNewBuffer();

        int channel = 0;
        foreach (float[] buffer in deinterleaved)
        {
            interleaver.AddBufferBytes(channel++, buffer.Length * sizeof(float));
        }

        interleaver.BuildNewBuffer();

        channel = 0;

        foreach (float[] buffer in deinterleaved)
        {
            interleaver.PutBytes(channel++, MemoryMarshal.Cast<float, byte>(buffer));
        }

        // Not required to call Drain here because all the buffers have the same size.

        ReadOnlySpan<byte> created = interleaver.GetBuffer();

        Assert.That(
            created.SequenceEqual(MemoryMarshal.Cast<float, byte>(originalInterleaved.AsSpan())),
            "The resulting buffer is not properly reinterleaved."
        );
    }

    /**
     * To assert that the interleaver can correctly handle cases under streaming (which is the typical use case),
     * we will use a random source to roll the number of bytes to copy for putting operations.
     * If, finally, the buffers do match, the test is successfull. The test will be repeated a couple of times
     * to ensure that we test the interleaver in different cases.
     */

    [Test]
    [Repeat(5)]
    public void TestAudioInterleaver_Streaming()
    {
        Random randomSource = new();

        float[][] deinterleaved;
        float[] originalInterleaved;

        (deinterleaved, originalInterleaved) = CreateSineWave(48000, 5, 1d, 440d);

        AudioInterleaver interleaver = new();

        interleaver.BytesPerFrame = sizeof(float); // 32-bit IEEE floating-point
        interleaver.Channels = deinterleaved.Length;

        // Keeps the bytes that have been copied from each buffer
        int[] usedBytes = new int[deinterleaved.Length];
        // Keeps bytes to copy for each copy run for each buffer
        int[] runTotalBytes = new int[deinterleaved.Length];

        int totalBytes = originalInterleaved.Length * sizeof(float);

        // Final buffer, we will compare this buffer against the contents of the originalInterleaved buffer.
        int targetIndex = 0;
        byte[] finalData = new byte[totalBytes];

        while (totalBytes > 0)
        {
            interleaver.PrepareNewBuffer();
            for (int I = 0; I < usedBytes.Length; I++)
            {
                int bytesForThisRun = Math.Min(randomSource.Next(1, 100) * sizeof(float), (deinterleaved[I].Length * sizeof(float)) - usedBytes[I]);
                interleaver.AddBufferBytes(I, bytesForThisRun);
                runTotalBytes[I] = bytesForThisRun;
                totalBytes -= bytesForThisRun;
            }
            interleaver.BuildNewBuffer();

            for (int I = 0; I < usedBytes.Length; I++)
            {
                interleaver.PutBytes(I, MemoryMarshal.Cast<float, byte>(deinterleaved[I]).Slice(usedBytes[I], runTotalBytes[I]));
                usedBytes[I] += runTotalBytes[I];
            }

            // Copy the used bytes for this run

            ReadOnlySpan<byte> dataSpan = interleaver.GetBuffer();

            dataSpan.CopyTo(finalData.AsSpan(targetIndex));

            targetIndex += dataSpan.Length;
        }

        while (interleaver.Drain())
        {
            // Drain the buffers...
            ReadOnlySpan<byte> dataSpan = interleaver.GetBuffer();

            dataSpan.CopyTo(finalData.AsSpan(targetIndex));

            targetIndex += dataSpan.Length;
        }

        // Now, compare the buffers.
        Assert.That(
            finalData.AsSpan().SequenceEqual(MemoryMarshal.Cast<float, byte>(originalInterleaved.AsSpan())),
            "The resulting buffer is not properly reinterleaved."
        );
    }
}
