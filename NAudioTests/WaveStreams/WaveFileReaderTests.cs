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
		[Category("UnitTest")]
		public void CanCreateReaderForStreamWithDataChunkGreaterThan2GB()
        {
            const int sampleBlockCount = Int32.MaxValue / (1024 * 3);
            const int channels = 2;
            const uint samplesPerBlock = 1024U * channels;
            const int bitsPerSample = 16;
            const uint bytesPerBlock = samplesPerBlock * (bitsPerSample / 8);
            const uint dataChunkLengthInBytes = sampleBlockCount * bytesPerBlock;
            Assert.IsTrue(dataChunkLengthInBytes > Int32.MaxValue && dataChunkLengthInBytes <= UInt32.MaxValue,
                "Something is wrong with the test set-up parameters.");
            using (var reader = new WaveFileReader(new LargeWaveStream(dataChunkLengthInBytes, new WaveFormat(8000, bitsPerSample, channels), new[] {0.1f, 0.2f})))
            {
                // Check the values in the first block.
                for (int i = 0; i < 1024; i++)
                {
                    var f1 = reader.ReadNextSampleFrame();
                    Assert.AreEqual(0.1f, f1[0], 0.0001f);
                    Assert.AreEqual(0.2f, f1[1], 0.0001f);
                }

                // Read the rest of the blocks in large chunks, except for the last one
                byte[] dataToReadAndIgnore = new byte[bytesPerBlock];
                for (int i = 0; i < sampleBlockCount - 2; i++)
                {
                    Assert.AreEqual(dataToReadAndIgnore.Length,
                        reader.Read(dataToReadAndIgnore, 0, dataToReadAndIgnore.Length),
                        "Failed to read block " + i + 1);
                }

                // Check the values in the last block.
                for (int i = 0; i < 1024; i++)
                {
                    var f1 = reader.ReadNextSampleFrame();
                    Assert.AreEqual(0.1f, f1[0], 0.0001f);
                    Assert.AreEqual(0.2f, f1[1], 0.0001f);
                }

                // Make sure there's no more data
                Assert.AreEqual(0, reader.Read(dataToReadAndIgnore, 0, 4));
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
                Assert.IsNotNull(ex);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }


	internal class LargeWaveStream : Stream
	{
		private const int HeaderLength = 46;
		private readonly long length;
		private byte[] header;
		private byte[] sampleData;

		public LargeWaveStream(uint dataLength, WaveFormat format, params float[] samples) :
			this(dataLength, format, ConvertFloatArrayToByteArray(format.BitsPerSample / 8, samples))
		{
		}

		public LargeWaveStream(uint dataLength, WaveFormat format, params byte[] bytes)
		{
			sampleData = bytes;
			MemoryStream headerStream = new MemoryStream(HeaderLength);
			using (new WaveFileWriter(headerStream, format))
			{
			}

			header = headerStream.ToArray();
			int dataPosition = header.Length - 4;
			byte[] dataLengthBytes = BitConverter.GetBytes(dataLength);
			for (int i = 0; i < dataLengthBytes.Length; i++)
				header[dataPosition + i] = dataLengthBytes[i];

			length = dataLength + HeaderLength;
		}

		private static byte[] ConvertFloatArrayToByteArray(int bytesPerSample, float[] samples)
		{
			byte[] bytes;
			switch (bytesPerSample)
			{
				case 2:
					bytes = new byte[2 * samples.Length];
					for (int i = 0; i < samples.Length; i++)
					{
						float sample = samples[i];
						Int16 val = (Int16)(Int16.MaxValue * sample);
						bytes[i * 2] = (byte)(val & 0xFF);
						bytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
					}
					break;
				default:
					throw new NotImplementedException(String.Format("We don't handle {0} bytes per sample.", bytesPerSample));
			}
			return bytes;
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			long position = Position; // Avoid extra member data access to shave off a few seconds.

			if (position < HeaderLength)
			{
				int headerBytesRead = 0;
				while (position < HeaderLength && count > 0)
				{
					buffer[offset + headerBytesRead++] = header[position++];
					count--;
				}
				Debug.Assert(count == 0, "Should never read any data along with header bytes");
				Position = position;
				return headerBytesRead;
			}

			count = (int)Math.Min(count, length - position);
			if (count == 0)
				return 0;

			int samLen = sampleData.Length;
			for (int i = 0; i < count; i++)
				buffer[i + offset] = sampleData[i % samLen];
			Position = position + count;
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override bool CanRead
		{
			get { return true; }
		}
		public override bool CanSeek
		{
			get { return false; }
		}
		public override bool CanWrite
		{
			get { return false; }
		}
		public override long Length
		{
			get { return length; }
		}
		public override long Position { get; set; }
	}
}
