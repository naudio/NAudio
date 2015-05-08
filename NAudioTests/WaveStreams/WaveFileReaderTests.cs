using System.Collections.Generic;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;
using System.Diagnostics;
using System;
using NAudio.FileFormats.Wav;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class WaveFileReaderTests
    {
        [Test]
        [Category("UnitTest")]
        public void TestEmptyFile()
        {
            // arrange
            var fileContents = new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // "RIFF"
                0x26, 0x00, 0x00, 0x00, // ChunkSize = 38
                0x57, 0x41, 0x56, 0x45, // "WAVE"
                0x66, 0x6d, 0x74, 0x20, // "fmt "
                0x12, 0x00, 0x00, 0x00, // Subchunk1Size = 18
                0x07, 0x00, 0x02, 0x00, // AudioFormat = 7, NumChannels = 2
                0x40, 0x1f, 0x00, 0x00, // SampleRate = 8000
                0x80, 0x3e, 0x00, 0x00, // ByteRate = 16000
                0x02, 0x00, 0x08, 0x00, // BlockAlign = 2, BitsPerSample = 8
                0x00, 0x00,             // ExtraParamSize = 0
                0x64, 0x61, 0x74, 0x61, // "data"
                0x00, 0x00, 0x00, 0x00, // Subchunk2Size = 0
            };
            using (var inputStream = new MemoryStream(fileContents))
            {
                // act
                var chunks = new List<RiffChunk>();
                var chunkReader = new WaveFileChunkReader();
                chunkReader.ReadWaveHeader(inputStream);

                // assert
                Assert.AreEqual(16000, chunkReader.WaveFormat.AverageBytesPerSecond);
                Assert.AreEqual(8, chunkReader.WaveFormat.BitsPerSample);
                Assert.AreEqual(2, chunkReader.WaveFormat.Channels);
                Assert.AreEqual(8000, chunkReader.WaveFormat.SampleRate);

                Assert.AreEqual(46, chunkReader.DataChunkPosition);
                Assert.AreEqual(0, chunkReader.DataChunkLength);
                Assert.AreEqual(0, chunks.Count);
            }
        }

        [Test]
        [Category("UnitTest")]
        public void CanAccessSamplesIndividuallyInAMonoFile()
        {
            var ms =  new MemoryStream();
            using (var writer = new WaveFileWriter( new IgnoreDisposeStream(ms), new WaveFormat(8000, 16, 1)))
            {
                writer.WriteSample(0.1f);
                writer.WriteSample(0.2f);
                writer.WriteSample(0.3f);
                writer.WriteSample(0.4f);
            }
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                Assert.AreEqual(0.1f, reader.ReadNextSampleFrame()[0], 0.001f);
                Assert.AreEqual(0.2f, reader.ReadNextSampleFrame()[0], 0.001f);
                Assert.AreEqual(0.3f, reader.ReadNextSampleFrame()[0], 0.001f);
                Assert.AreEqual(0.4f, reader.ReadNextSampleFrame()[0], 0.001f);
                Assert.IsNull(reader.ReadNextSampleFrame());
            }
        }

        [Test]
        [Category("UnitTest")]
        public void CanAccessSamplesIndividuallyInAStereoFile()
        {
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(8000, 16, 2)))
            {
                writer.WriteSample(0.1f);
                writer.WriteSample(0.2f);
                writer.WriteSample(0.3f);
                writer.WriteSample(0.4f);

            }
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                var f1 = reader.ReadNextSampleFrame();
                Assert.AreEqual(0.1f, f1[0], 0.0001f);
                Assert.AreEqual(0.2f, f1[1], 0.0001f);
                var f2 = reader.ReadNextSampleFrame();
                Assert.AreEqual(0.3f, f2[0], 0.0001f);
                Assert.AreEqual(0.4f, f2[1], 0.0001f);
                Assert.IsNull(reader.ReadNextSampleFrame());
            }
        }

        [Test]
        [Category("UnitTest")]
        public void CanAccessSamplesIndividuallyInAStereo24BitFile()
        {
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(44100, 24, 2)))
            {
                writer.WriteSample(0.1f);
                writer.WriteSample(0.2f);
                writer.WriteSample(0.3f);
                writer.WriteSample(0.4f);

            }
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                var f1 = reader.ReadNextSampleFrame();
                Assert.AreEqual(0.1f, f1[0], 0.0001f);
                Assert.AreEqual(0.2f, f1[1], 0.0001f);
                var f2 = reader.ReadNextSampleFrame();
                Assert.AreEqual(0.3f, f2[0], 0.0001f);
                Assert.AreEqual(0.4f, f2[1], 0.0001f);
                Assert.IsNull(reader.ReadNextSampleFrame());
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanLoadAndReadVariousProblemWavFiles()
        {
            string testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
            if (!Directory.Exists(testDataFolder))
            {
                Assert.Ignore("{0} not found", testDataFolder);
            }
            foreach (string file in Directory.GetFiles(testDataFolder, "*.wav"))
            {
                string wavFile = Path.Combine(testDataFolder, file);
                Debug.WriteLine(String.Format("Opening {0}", wavFile));
                using (var reader = new WaveFileReader(wavFile))
                {
                    byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                    int bytesRead;
                    int total = 0;
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        total += bytesRead;
                    } while (bytesRead > 0);
                    Debug.WriteLine(String.Format("Read {0} bytes", total));
                }
            }
        }

        [Test]
        [Category("UnitTest")]
        public void DisposeOfStreamWhenConstructedFromFilePath()
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, "Some test content");
            try
            {
                WaveFileReader waveReader = new WaveFileReader(tempFilePath);

                Assert.Fail("Expected exception System.FormatException was not thrown for file missing a header.");
            }
            catch(FormatException ex)
            {
                Assert.IsNotNull(ex);
            }
            finally
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }
}
