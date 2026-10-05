
using System;
using System.Threading;
using System.Runtime.InteropServices;

using NUnit.Framework;

using NAudio.Utils;
using NAudio.Wave.SampleProviders;


namespace NAudio.MacOS.Tests;

[TestFixture]
public class UtilsAudioBufferTests
{
    private static Span<byte> CreateSineWave(int sampleRate, int channels, double durationSeconds, double frequency)
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
        return MemoryMarshal.Cast<float, byte>(sampleBuffer);
    }

    [Test]
    public void TestThatBufferReturnsTheSameData()
    {
        Span<byte> signal = CreateSineWave(48000, 1, 1d, 440d);

        byte[] reconstructed = new byte[signal.Length];

        // Simulate copy: copy the data as 512-byte chunks to the buffer.

        AudioBuffer buffer = new();

        int consumed = 0;

        while (consumed < signal.Length)
        {
            buffer.EnsureBytes(512);
            Span<byte> sliced = signal.Slice(consumed, Math.Min(signal.Length - consumed, 512));
            buffer.PutBytes(sliced);
            consumed += sliced.Length;
        }

        // Now get the data back.
        int read = 0;
        consumed = 0;
        while (true)
        {
            read = buffer.GetBytes(reconstructed.AsSpan(consumed, Math.Min(signal.Length - consumed, 512)));
            if (read == 0) { break; }
            consumed += read;
        }

        Assert.That(consumed, Is.EqualTo(signal.Length), "The vaild data in the buffer are less than the signal's data.");

        Assert.That(signal.SequenceEqual(reconstructed), "The two buffer contents do not match.");
    }

    /**
     * To assert that the buffer can correctly handle cases under streaming (which is the typical use case),
     * we will use a random source to roll a number of iterations for putting/draining operations. 
     * If, finally, the buffers do match, the test is successfull. The test will be repeated a couple of times
     * to ensure that we test the buffer in different cases.
     */
    [Test]
    [Repeat(10)]
    public void TestThatBufferReturnsTheSameData_Streaming()
    {
        Random randomSource = new Random();

        Span<byte> signal = CreateSineWave(48000, 1, 1d, 440d);

        byte[] reconstructed = new byte[signal.Length];

        AudioBuffer buffer = new();

        int read = 0;
        int iterations = 0;
        int targetConsumed = 0;
        int sourceConsumed = 0;

        // Hard limit of 15 seconds, if the buffer is misbehaving.
        // This is used to stop the loop so that we do not get stuck to an infinite loop
        CancellationTokenSource source = new(new TimeSpan(0, 0, 15));

        while ((sourceConsumed < signal.Length || targetConsumed < signal.Length) && (!source.IsCancellationRequested))
        {
            iterations = randomSource.Next(1, 5);
            while (sourceConsumed < signal.Length && iterations-- > 0)
            {
                buffer.EnsureBytes(512);
                Span<byte> sliced = signal.Slice(sourceConsumed, Math.Min(signal.Length - sourceConsumed, 512));
                buffer.PutBytes(sliced);
                sourceConsumed += sliced.Length;
            }
            iterations = randomSource.Next(1, 5);
            while (iterations-- > 0)
            {
                // Make a buffer of random size to even further simulate streaming.
                read = buffer.GetBytes(reconstructed.AsSpan(targetConsumed, Math.Min(signal.Length - targetConsumed, randomSource.Next(128, 1024))));
                if (read == 0) { break; }
                targetConsumed += read;
            }
        }

        Assert.That(source.IsCancellationRequested, Is.False, "Test failed because it took too long for the buffer to process the data.");

        Assert.That(sourceConsumed, Is.EqualTo(signal.Length), "The entire source buffer was not consumed!");

        Assert.That(targetConsumed, Is.EqualTo(signal.Length), "The vaild data in the buffer are less than the signal's data.");

        Assert.That(signal.SequenceEqual(reconstructed), "The two buffer contents do not match.");
    }
}
