using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class SampleToWaveProvider24Tests
    {
        [Test]
        public void ConvertAFile()
        {
            const string input = @"C:\Users\Mark\Downloads\Region-1.wav";
            if (!File.Exists(input)) Assert.Ignore("Test file not found");
            using (var reader = new WaveFileReader(input))
            {
                var sp = reader.ToSampleProvider();
                var wp24 = new SampleToWaveProvider24(sp);
                WaveFileWriter.CreateWaveFile(@"C:\Users\Mark\Downloads\Region1-24.wav", wp24);
            }
        }
    }
}
