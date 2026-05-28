using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;
using NAudio.Tests.Shared;
using NAudio.Windows.Tests.Utils;

namespace NAudio.Windows.Tests.Dmo
{
    [TestFixture]
    public class ResamplerDmoStreamTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateResamplerStream()
        {
            //using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            using (WaveStream reader = new NullWaveStream(new WaveFormat(44100,16,1),1000 ))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000,2)))
                {
                    Assert.That(resampler.Length, Is.GreaterThan(reader.Length), "Length");
                    Assert.That(reader.Position, Is.EqualTo(0), "Position");
                    Assert.That(resampler.Position, Is.EqualTo(0), "Position");            
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanReadABlockFromResamplerStream()
        {
            //using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            WaveFormat inputFormat = new WaveFormat(44100, 16, 1);
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 20))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)))
                {
                    // try to read 10 ms;
                    int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    int count = resampler.Read(buffer, 0, bytesToRead);
                    Assert.That(count, Is.GreaterThan(0), "Bytes Read");
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamToIEEE()
        {
            WaveFormat inputFormat = new WaveFormat(44100, 16, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo48000PCM()
        {
            WaveFormat inputFormat = new WaveFormat(44100, 16, 2);
            WaveFormat outputFormat = new WaveFormat(48000, 16, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }


        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo44100IEEE()
        {
            WaveFormat inputFormat = new WaveFormat(48000, 16, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanResampleAWholeStreamTo44100PCM()
        {
            WaveFormat inputFormat = new WaveFormat(48000, 16, 2);
            WaveFormat outputFormat = new WaveFormat(44100, 16, 2);
            ResampleAWholeStream(inputFormat, outputFormat);
        }

        private void ResampleAWholeStream(WaveFormat inputFormat, WaveFormat outputFormat)
        {
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 20))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, outputFormat))
                {
                    // try to read 10 ms;
                    int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    int count;
                    int total = 0;
                    do
                    {
                        count = resampler.Read(buffer, 0, bytesToRead);
                        total += count;
                        //Assert.AreEqual(count, bytesToRead, "Bytes Read");
                    } while (count > 0);
                    //Debug.WriteLine(String.Format("Converted input length {0} to {1}", reader.Length, total));
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        [CancelAfter(5000)]
        public void CanReadAfterSeekingBackToStart()
        {
            // Regression: setting Position calls Discontinuity which used to wedge the next
            // Read in an infinite loop because the "not accepting data" branch was a no-op.
            WaveFormat inputFormat = new WaveFormat(44100, 16, 1);
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 5))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)))
                {
                    int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];

                    int first = resampler.Read(buffer, 0, bytesToRead);
                    Assert.That(first, Is.GreaterThan(0), "Bytes Read before seek");

                    resampler.Position = 0;
                    Assert.That(resampler.Position, Is.EqualTo(0), "Position after seek");

                    int second = resampler.Read(buffer, 0, bytesToRead);
                    Assert.That(second, Is.GreaterThan(0), "Bytes Read after seek");
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void DrainsTailSamplesAtEndOfStream()
        {
            // Regression: at EOS the resampler used to break out of Read without telling
            // the DMO, losing the ~32-sample tail still inside its quality-30 kernel.
            WaveFormat inputFormat = new WaveFormat(44100, 16, 2);
            WaveFormat outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 2))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, outputFormat))
                {
                    long expected = resampler.Length;
                    int bytesToRead = outputFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    long total = 0;
                    int count;
                    do
                    {
                        count = resampler.Read(buffer, 0, bytesToRead);
                        total += count;
                    } while (count > 0);

                    // Allow one output block of slack - the DMO's resampler kernel rounds
                    // off the very last samples, and one BlockAlign is within that tolerance.
                    Assert.That(total, Is.GreaterThanOrEqualTo(expected - outputFormat.BlockAlign),
                        $"Drained total {total} should match expected length {expected} within one BlockAlign");
                    Assert.That(total, Is.LessThanOrEqualTo(expected + outputFormat.BlockAlign),
                        $"Drained total {total} should not exceed expected length {expected} by more than one BlockAlign");

                    int afterEos = resampler.Read(buffer, 0, bytesToRead);
                    Assert.That(afterEos, Is.EqualTo(0), "Reads after EOS should return 0");
                }
            }
        }

        /*[Test]
        public void CanResampleToWav()
        {
            using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, new WaveFormat(48000, 16, 2)))
                {
                    using (WaveFileWriter writer = new WaveFileWriter("C:\\Users\\Mark\\Recording\\REAPER\\ideas-converted.wav", resampler.WaveFormat))
                    {
                        // try to read 10 ms;
                        int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                        byte[] buffer = new byte[bytesToRead];
                        int count;
                        int total = 0;
                        do
                        {
                            count = resampler.Read(buffer, 0, bytesToRead);
                            writer.WriteData(buffer, 0, count);
                            total += count;
                            //Assert.AreEqual(count, bytesToRead, "Bytes Read");
                        } while (count > 0);
                        Debug.WriteLine(String.Format("Converted input length {0} to {1}", reader.Length, total));
                    }
                }
            }
        }*/
    }
}
