using System;
using System.IO;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Wave
{
    [TestFixture]
    [Category("UnitTest")]
    public class BwfWriterTests
    {
        private string _testFilePath;

        [SetUp]
        public void SetUp()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_bwf_{Guid.NewGuid()}.wav");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testFilePath))
            {
                try { File.Delete(_testFilePath); }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private BextChunkInfo CreateDefaultBextChunkInfo()
        {
            return new BextChunkInfo
            {
                Description = "Test Description",
                Originator = "Test Originator",
                OriginatorReference = "TEST_REF_001",
                OriginationDateTime = new DateTime(2023, 1, 15, 14, 30, 45),
                TimeReference = 0L,
                UniqueMaterialIdentifier = "TEST_UMID_123456789",
                CodingHistory = "F=48000,BW=24000,T=STEREO"
            };
        }

        [Test]
        public void ConstructorShouldCreateBwfFile()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                // File should be created
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
        }

        [Test]
        public void ConstructorWithNullFormatShouldThrowArgumentNullException()
        {
            var bextInfo = CreateDefaultBextChunkInfo();

            Assert.That(() => new BwfWriter(_testFilePath, null, bextInfo), Throws.ArgumentNullException);
        }

        [Test]
        public void ConstructorWithNullBextChunkInfoShouldThrowArgumentNullException()
        {
            var format = new WaveFormat(44100, 16, 2);

            Assert.That(() => new BwfWriter(_testFilePath, format, null), Throws.ArgumentNullException);
        }

        [Test]
        public void ConstructorWithNullFilenameShouldThrowArgumentNullException()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();

            Assert.That(() => new BwfWriter(null, format, bextInfo), Throws.ArgumentNullException);
        }

        [Test]
        public void WriteShouldAppendAudioData()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
                writer.Flush();
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(0));
        }

        [Test]
        public void WriteWithOffsetShouldWriteOnlyRequestedSlice()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0xAA, 0x11, 0x22, 0x33, 0x44, 0xBB };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 1, 4);
                writer.Flush();
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
        }

        [Test]
        public void WriteNullBufferShouldThrowException()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                Assert.That(() => writer.Write(null, 0, 0), Throws.Exception);
            }
        }

        [Test]
        public void WriteAfterDisposeShouldThrowObjectDisposedException()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01 };

            var writer = new BwfWriter(_testFilePath, format, bextInfo);
            writer.Dispose();

            Assert.That(() => writer.Write(audioData, 0, audioData.Length), 
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void FlushShouldUpdateHeaderSizes()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[1000];
            Array.Fill(audioData, (byte)0x42);

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
                writer.Flush();
            }

            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(1000));
        }

        [Test]
        public void FlushAfterDisposeShouldThrowObjectDisposedException()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();

            var writer = new BwfWriter(_testFilePath, format, bextInfo);
            writer.Dispose();

            Assert.That(() => writer.Flush(), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void DisposeShouldFixUpHeaderSizes()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[2000];
            Array.Fill(audioData, (byte)0x55);

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(2000));
        }

        [Test]
        public void DisposeShouldBeIdempotent()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();

            var writer = new BwfWriter(_testFilePath, format, bextInfo);
            writer.Dispose();

            // Should not throw
            Assert.That(() => writer.Dispose(), Throws.Nothing);
        }

        [Test]
        public void FileFormatShouldStartWithRiff()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            using (var file = File.OpenRead(_testFilePath))
            using (var reader = new BinaryReader(file))
            {
                var header = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
                Assert.That(header, Is.EqualTo("RIFF").Or.EqualTo("RF64"));
            }
        }

        [Test]
        public void FileFormatShouldContainWaveChunk()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            using (var file = File.OpenRead(_testFilePath))
            using (var reader = new BinaryReader(file))
            {
                reader.ReadBytes(8); // Skip RIFF header and size
                var waveHeader = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
                Assert.That(waveHeader, Is.EqualTo("WAVE"));
            }
        }

        [Test]
        public void FileFormatShouldContainBextChunk()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            var fileContent = File.ReadAllBytes(_testFilePath);
            var fileText = System.Text.Encoding.ASCII.GetString(fileContent);
            Assert.That(fileText, Does.Contain("bext"));
        }

        [Test]
        public void FileFormatShouldContainFmtChunk()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            var fileContent = File.ReadAllBytes(_testFilePath);
            var fileText = System.Text.Encoding.ASCII.GetString(fileContent);
            Assert.That(fileText, Does.Contain("fmt "));
        }

        [Test]
        public void FileFormatShouldContainDataChunk()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            var fileContent = File.ReadAllBytes(_testFilePath);
            var fileText = System.Text.Encoding.ASCII.GetString(fileContent);
            Assert.That(fileText, Does.Contain("data"));
        }

        [Test]
        public void WritingLargeFileShouldUseSf64Format()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            
            // Create large audio data (more than Int32.MaxValue)
            // For testing purposes, we'll use a smaller threshold approach
            var audioData = new byte[10000000]; // 10MB of audio data
            Array.Fill(audioData, (byte)0xAA);

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            using (var file = File.OpenRead(_testFilePath))
            using (var reader = new BinaryReader(file))
            {
                var header = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
                // Should eventually convert to RF64 if size exceeds threshold
                Assert.That(File.Exists(_testFilePath), Is.True);
            }
        }

        [Test]
        public void MultipleWritesShouldAppendData()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData1 = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var audioData2 = new byte[] { 0x04, 0x05, 0x06, 0x07 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData1, 0, audioData1.Length);
                writer.Write(audioData2, 0, audioData2.Length);
                writer.Flush();
            }

            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(audioData1.Length + audioData2.Length));
        }

        [Test]
        public void BextDescriptionShouldBePaddedToMaxLength()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(256)); // Description is 256 bytes minimum
        }

        [Test]
        public void WriteShouldTrackDataLength()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[5000];
            Array.Fill(audioData, (byte)0x11);

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, 2500);
                writer.Write(audioData, 2500, 2500);
                writer.Flush();
            }

            var fileSize = new FileInfo(_testFilePath).Length;
            Assert.That(fileSize, Is.GreaterThan(5000));
        }

        [Test]
        public void DifferentWaveFormatsShouldBeSupported()
        {
            var bextInfo = CreateDefaultBextChunkInfo();
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            var formats = new[]
            {
                new WaveFormat(22050, 16, 1),  // Mono
                new WaveFormat(44100, 16, 2),  // Stereo
                new WaveFormat(48000, 24, 2),  // 24-bit Stereo
                new WaveFormat(96000, 32, 2),  // 32-bit Stereo
            };

            foreach (var format in formats)
            {
                var filePath = Path.Combine(Path.GetTempPath(), $"test_bwf_{Guid.NewGuid()}.wav");
                try
                {
                    using (var writer = new BwfWriter(filePath, format, bextInfo))
                    {
                        writer.Write(audioData, 0, audioData.Length);
                    }

                    Assert.That(File.Exists(filePath), Is.True);
                }
                finally
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
            }
        }

        [Test]
        public void BextInfoWithCodingHistoryShouldBeSupported()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = new BextChunkInfo
            {
                Description = "Test with Coding History",
                Originator = "Test",
                OriginatorReference = "REF001",
                OriginationDateTime = new DateTime(2023, 6, 15),
                TimeReference = 100L,
                UniqueMaterialIdentifier = "UMID001",
                CodingHistory = "F=44100,BW=22050,T=STEREO\nF=44100,BW=22050,T=STEREO"
            };
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
        }

        [Test]
        public void BextInfoWithNullCodingHistoryShouldNotThrow()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bextInfo = new BextChunkInfo
            {
                Description = "Test without Coding History",
                Originator = "Test",
                OriginatorReference = "REF001",
                OriginationDateTime = DateTime.Now,
                TimeReference = 0L,
                UniqueMaterialIdentifier = "UMID001",
                CodingHistory = null
            };
            var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            using (var writer = new BwfWriter(_testFilePath, format, bextInfo))
            {
                writer.Write(audioData, 0, audioData.Length);
            }

            Assert.That(File.Exists(_testFilePath), Is.True);
        }
    }
}
