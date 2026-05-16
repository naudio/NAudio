using System;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests
{
    /// <summary>
    /// Skips a fixture unless it is running on Linux with a usable
    /// <c>libasound</c>. Tests derived from this exercise the ALSA
    /// <c>null</c> PCM, which is pure user space (no sound hardware).
    /// </summary>
    public abstract class AlsaTestBase
    {
        [OneTimeSetUp]
        public void RequireAlsa()
        {
            if (!OperatingSystem.IsLinux())
            {
                Assert.Ignore("ALSA tests require Linux");
            }

            try
            {
                // Forces the libasound resolver; throws DllNotFoundException
                // if neither libasound.so.2 nor libasound.so is present.
                AlsaException.ThrowIfError(0, "probe");
            }
            catch (DllNotFoundException)
            {
                Assert.Ignore("libasound is not installed");
            }
        }

        /// <summary>A short finite tone used to drive playback tests.</summary>
        protected sealed class ToneProvider : IWaveProvider
        {
            private long remaining = 44100L * 4 / 4; // ~0.25s stereo s16
            private double phase;

            public WaveFormat WaveFormat { get; } = new(44100, 16, 2);

            public int Read(Span<byte> buffer)
            {
                if (remaining <= 0)
                {
                    return 0;
                }

                int count = (int)Math.Min(buffer.Length & ~3, remaining);
                for (int i = 0; i < count; i += 4)
                {
                    short s = (short)(Math.Sin(phase) * 8000);
                    phase += 2 * Math.PI * 440 / 44100;
                    buffer[i] = (byte)s;
                    buffer[i + 1] = (byte)(s >> 8);
                    buffer[i + 2] = (byte)s;
                    buffer[i + 3] = (byte)(s >> 8);
                }

                remaining -= count;
                return count;
            }
        }
    }
}
