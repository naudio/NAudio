using System;
using NAudio.SoundFile;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.SoundFile.Tests
{
    /// <summary>
    /// Skips the fixture unless a usable libsndfile is present. libsndfile is
    /// cross-platform, so (unlike the ALSA suite) there is no OS gate — only
    /// a native-library probe.
    /// </summary>
    public abstract class SoundFileTestBase
    {
        [OneTimeSetUp]
        public void RequireSoundFile()
        {
            try
            {
                // Forces the libsndfile resolver; throws DllNotFoundException
                // if no libsndfile variant is on the system.
                _ = SoundFileCapabilities.GetSupportedMajorFormats();
            }
            catch (DllNotFoundException)
            {
                Assert.Ignore("libsndfile is not installed");
            }
        }

        /// <summary>Skips the test if the libsndfile build lacks a codec.</summary>
        protected static void RequireFormat(SoundFileMajorFormat major)
        {
            if (!SoundFileCapabilities.IsFormatSupported(major))
            {
                Assert.Ignore($"libsndfile build does not support {major}");
            }
        }

        /// <summary>A finite interleaved 440 Hz sine, 16-bit PCM stereo.</summary>
        protected sealed class TonePcm16 : IWaveProvider
        {
            private long remaining;
            private double phase;

            public TonePcm16(int sampleRate = 48000, double seconds = 0.5)
            {
                WaveFormat = new WaveFormat(sampleRate, 16, 2);
                remaining = (long)(WaveFormat.AverageBytesPerSecond * seconds) & ~3;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(Span<byte> buffer)
            {
                if (remaining <= 0)
                {
                    return 0;
                }
                int count = (int)Math.Min(buffer.Length & ~3, remaining);
                double step = 2 * Math.PI * 440 / WaveFormat.SampleRate;
                for (int i = 0; i < count; i += 4)
                {
                    short s = (short)(Math.Sin(phase) * 8000);
                    phase += step;
                    buffer[i] = (byte)s;
                    buffer[i + 1] = (byte)(s >> 8);
                    buffer[i + 2] = (byte)s;
                    buffer[i + 3] = (byte)(s >> 8);
                }
                remaining -= count;
                return count;
            }
        }

        /// <summary>Root-mean-square of an interleaved float buffer.</summary>
        protected static double Rms(ReadOnlySpan<float> samples)
        {
            if (samples.Length == 0)
            {
                return 0;
            }
            double sum = 0;
            foreach (var s in samples)
            {
                sum += (double)s * s;
            }
            return Math.Sqrt(sum / samples.Length);
        }
    }
}
