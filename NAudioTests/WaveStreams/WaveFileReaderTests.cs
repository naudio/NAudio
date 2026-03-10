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
                Assert.That(chunkReader.WaveFormat.AverageBytesPerSecond, Is.EqualTo(16000));
                Assert.That(chunkReader.WaveFormat.BitsPerSample, Is.EqualTo(8));
                Assert.That(chunkReader.WaveFormat.Channels, Is.EqualTo(2));
                Assert.That(chunkReader.WaveFormat.SampleRate, Is.EqualTo(8000));

                Assert.That(chunkReader.DataChunkPosition, Is.EqualTo(46));
                Assert.That(chunkReader.DataChunkLength, Is.EqualTo(0));
                Assert.That(chunks.Count, Is.EqualTo(0));
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
                Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.3f).Within(0.001f));
                Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(reader.ReadNextSampleFrame(), Is.Null);
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
                Assert.That(f1[0], Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(f1[1], Is.EqualTo(0.2f).Within(0.0001f));
                var f2 = reader.ReadNextSampleFrame();
                Assert.That(f2[0], Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(f2[1], Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(reader.ReadNextSampleFrame(), Is.Null);
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
                Assert.That(f1[0], Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(f1[1], Is.EqualTo(0.2f).Within(0.0001f));
                var f2 = reader.ReadNextSampleFrame();
                Assert.That(f2[0], Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(f2[1], Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(reader.ReadNextSampleFrame(), Is.Null);
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanLoadAndReadVariousProblemWavFiles()
        {
            string testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
            if (!Directory.Exists(testDataFolder))
            {
                Assert.Ignore($"{testDataFolder} not found");
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
                Assert.That(ex, Is.Not.Null);
            }
            finally
            {
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void Mp3FileReaderDisposesFileOnFailToParse()
        {
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Some test content");
            try
            {
                var reader = new Mp3FileReader(tempFilePath);

                Assert.Fail("Expected exception System.FormatException was not thrown for file missing a header.");
            }
            catch (InvalidDataException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
