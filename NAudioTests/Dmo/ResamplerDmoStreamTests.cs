using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Dmo
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
                    Assert.Greater(resampler.Length, reader.Length, "Length");
                    Assert.AreEqual(0, reader.Position, "Position");
                    Assert.AreEqual(0, resampler.Position, "Position");            
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
                    Assert.That(count > 0, "Bytes Read");
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
