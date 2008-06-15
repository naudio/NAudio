using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.Acm
{
    [TestFixture]
    public class WaveFormatConversionStreamTests
    {
        [Test]
        public void CanConvertPcmToMuLaw()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw,
                    channels,
                    sampleRate,
                    sampleRate * channels,
                    1, 8));
        }

        [Test]
        public void CanConvertPcmToALaw()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw,
                    channels,
                    sampleRate,
                    sampleRate * channels,
                    1, 8));
        }

        [Test]
        public void CanConvertALawToPcm()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw,
                    channels,
                    sampleRate,
                    sampleRate * channels,
                    1, 8),
                new WaveFormat(sampleRate, 16, channels));
        }

        [Test]
        public void CanConvertMuLawToPcm()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                WaveFormat.CreateCustomFormat(WaveFormatEncoding.MuLaw,
                    channels,
                    sampleRate,
                    sampleRate * channels,
                    1, 8),
                new WaveFormat(sampleRate, 16, channels));
        }

        [Test]
        public void CanConvertAdpcmToPcm()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormatAdpcm(8000,1),
                new WaveFormat(sampleRate, 16, channels));
        }

        [Test]
        public void CanConvertAdpcmToSuggestedPcm()
        {
            using(WaveStream stream = WaveFormatConversionStream.CreatePcmStream(
                new NullWaveStream(new WaveFormatAdpcm(8000, 1))))
                {
            }
        }

        [Test]
        public void CanConvertPcmToAdpcm()
        {
            int channels = 1;
            int sampleRate = 8000;
            CanCreateConversionStream(
                new WaveFormat(sampleRate, 16, channels),
                new WaveFormatAdpcm(8000, 1));
        }

        private void CanCreateConversionStream(WaveFormat inputFormat, WaveFormat outputFormat)
        {
            using (WaveFormatConversionStream stream = new WaveFormatConversionStream(
                inputFormat, new NullWaveStream(outputFormat)))
            {
            }
        }
    }

    class NullWaveStream : WaveStream
    {
        WaveFormat format;
        long position = 0;
        public NullWaveStream(WaveFormat format)
        {
            this.format = format;
        }

        public override WaveFormat WaveFormat
        {
            get { return format; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            position += count;
            return count;
        }
    }
}

