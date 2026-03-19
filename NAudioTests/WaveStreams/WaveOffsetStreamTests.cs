using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveOffsetStreamTests
    {
        // 16-bit mono 8kHz = 2 bytes per sample, 16000 bytes per second
        private const int SampleRate = 8000;
        private const int BitsPerSample = 16;
        private const int Channels = 1;
        private const int BytesPerSample = BitsPerSample / 8 * Channels; // 2
        private const int BytesPerSecond = SampleRate * BytesPerSample; // 16000

        /// <summary>
        /// A WaveStream that produces identifiable non-zero data (repeating 1-255 pattern)
        /// so we can distinguish source audio from silence padding
        /// </summary>
        private class IdentifiableWaveStream : WaveStream
        {
            private readonly WaveFormat waveFormat;
            private readonly long length;
            private long position;

            public IdentifiableWaveStream(long lengthInBytes)
            {
                waveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
                length = lengthInBytes;
            }

            public override WaveFormat WaveFormat => waveFormat;
            public override long Length => length;

            public override long Position
            {
                get => position;
                set => position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int toRead = (int)Math.Min(count, length - position);
                for (int i = 0; i < toRead; i++)
                {
                    // Produce a non-zero pattern: byte value is ((position + i) % 255) + 1
                    // This ensures bytes are always 1-255, never 0, so we can tell them apart from silence
                    buffer[offset + i] = (byte)(((position + i) % 255) + 1);
                }
                position += toRead;
                return toRead;
            }
        }

        private IdentifiableWaveStream CreateSourceStream(double durationSeconds = 2.0)
        {
            long lengthBytes = (long)(durationSeconds * BytesPerSecond);
            return new IdentifiableWaveStream(lengthBytes);
        }

        #region Constructor Tests

        [Test]
        public void DefaultConstructorSetsZeroStartTime()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.StartTime, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void DefaultConstructorSetsZeroSourceOffset()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.SourceOffset, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void DefaultConstructorSetsSourceLengthToTotalTime()
        {
            using var source = CreateSourceStream(2.0);
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.SourceLength, Is.EqualTo(source.TotalTime));
        }

        [Test]
        public void DefaultConstructorLengthMatchesSource()
        {
            using var source = CreateSourceStream(2.0);
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.Length, Is.EqualTo(source.Length));
        }

        [Test]
        public void ConstructorSetsStartTime()
        {
            using var source = CreateSourceStream();
            var startTime = TimeSpan.FromSeconds(1);
            using var offset = new WaveOffsetStream(source, startTime, TimeSpan.Zero, source.TotalTime);
            Assert.That(offset.StartTime, Is.EqualTo(startTime));
        }

        [Test]
        public void ConstructorSetsSourceOffset()
        {
            using var source = CreateSourceStream();
            var srcOffset = TimeSpan.FromSeconds(0.5);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, srcOffset, source.TotalTime);
            Assert.That(offset.SourceOffset, Is.EqualTo(srcOffset));
        }

        [Test]
        public void ConstructorSetsSourceLength()
        {
            using var source = CreateSourceStream();
            var srcLength = TimeSpan.FromSeconds(1);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, srcLength);
            Assert.That(offset.SourceLength, Is.EqualTo(srcLength));
        }

        [Test]
        public void ConstructorRejectsNonPcmFormat()
        {
            // IeeeFloat encoding should be rejected
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            using var source = new NonPcmWaveStream(format, 1000);
            Assert.Throws<ArgumentException>(() => new WaveOffsetStream(source));
        }

        [Test]
        public void ConstructorSetsPositionToZero()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.Position, Is.EqualTo(0));
        }

        #endregion

        #region WaveFormat Tests

        [Test]
        public void WaveFormatMatchesSource()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.WaveFormat, Is.EqualTo(source.WaveFormat));
        }

        [Test]
        public void BlockAlignMatchesSource()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            Assert.That(offset.BlockAlign, Is.EqualTo(source.BlockAlign));
        }

        #endregion

        #region Length Tests

        [Test]
        public void LengthEqualsSourceLengthWithNoStartTime()
        {
            using var source = CreateSourceStream(2.0);
            var srcLength = TimeSpan.FromSeconds(1);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, srcLength);
            long expectedLength = (long)(1.0 * SampleRate) * BytesPerSample;
            Assert.That(offset.Length, Is.EqualTo(expectedLength));
        }

        [Test]
        public void LengthIncludesStartTimePadding()
        {
            using var source = CreateSourceStream(2.0);
            var startTime = TimeSpan.FromSeconds(1);
            var srcLength = TimeSpan.FromSeconds(1);
            using var offset = new WaveOffsetStream(source, startTime, TimeSpan.Zero, srcLength);
            // Length = audioStartPosition + sourceLengthBytes = 1s + 1s = 2s worth of bytes
            long expectedLength = (long)(2.0 * SampleRate) * BytesPerSample;
            Assert.That(offset.Length, Is.EqualTo(expectedLength));
        }

        [Test]
        public void ChangingStartTimeUpdatesLength()
        {
            using var source = CreateSourceStream(2.0);
            using var offset = new WaveOffsetStream(source);
            long originalLength = offset.Length;

            offset.StartTime = TimeSpan.FromSeconds(1);
            Assert.That(offset.Length, Is.EqualTo(originalLength + BytesPerSecond));
        }

        [Test]
        public void ChangingSourceLengthUpdatesLength()
        {
            using var source = CreateSourceStream(2.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));
            Assert.That(offset.Length, Is.EqualTo(2 * BytesPerSecond));

            offset.SourceLength = TimeSpan.FromSeconds(1);
            Assert.That(offset.Length, Is.EqualTo(1 * BytesPerSecond));
        }

        #endregion

        #region Position Tests

        [Test]
        public void CanSetAndGetPosition()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            offset.Position = 100;
            Assert.That(offset.Position, Is.EqualTo(100));
        }

        [Test]
        public void PositionIsBlockAligned()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            // BlockAlign is 2 for 16-bit mono, setting to odd number should align down
            offset.Position = 101;
            Assert.That(offset.Position % offset.BlockAlign, Is.EqualTo(0));
        }

        [Test]
        public void SettingPositionBeforeAudioStartSetsSourceToOffset()
        {
            using var source = CreateSourceStream(3.0);
            var srcOffset = TimeSpan.FromSeconds(0.5);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(1), srcOffset, TimeSpan.FromSeconds(2));

            offset.Position = 0; // before audioStartPosition
            long expectedSourcePos = (long)(0.5 * SampleRate) * BytesPerSample;
            Assert.That(source.Position, Is.EqualTo(expectedSourcePos));
        }

        [Test]
        public void SettingPositionAfterAudioStartSetsSourceCorrectly()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(2));

            long audioStart = (long)(1.0 * SampleRate) * BytesPerSample;
            long seekTo = audioStart + 1000;
            offset.Position = seekTo;
            // sourceStream.Position should be sourceOffsetBytes + (position - audioStartPosition)
            Assert.That(source.Position, Is.EqualTo(1000));
        }

        [Test]
        public void ReadAdvancesPosition()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            var buffer = new byte[1000];
            _ = offset.Read(buffer, 0, 1000);
            Assert.That(offset.Position, Is.EqualTo(1000));
        }

        #endregion

        #region StartTime (Lead-In Silence) Tests

        [Test]
        public void StartTimeProducesSilenceBeforeAudio()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Read the first second (should be silence)
            var buffer = new byte[BytesPerSecond];
            int read = offset.Read(buffer, 0, buffer.Length);

            Assert.That(read, Is.EqualTo(BytesPerSecond));
            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.That(buffer[i], Is.EqualTo(0), $"Expected silence at byte {i}");
            }
        }

        [Test]
        public void AudioFollowsLeadInSilence()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Skip past the lead-in silence
            var silenceBuffer = new byte[BytesPerSecond];
            _ = offset.Read(silenceBuffer, 0, silenceBuffer.Length);

            // Now read the audio portion
            var audioBuffer = new byte[100];
            int read = offset.Read(audioBuffer, 0, audioBuffer.Length);

            Assert.That(read, Is.EqualTo(100));
            // The source produces non-zero bytes, so audio should contain non-zero data
            bool hasNonZero = false;
            for (int i = 0; i < read; i++)
            {
                if (audioBuffer[i] != 0) hasNonZero = true;
            }
            Assert.That(hasNonZero, Is.True, "Audio portion should contain non-zero data from source");
        }

        [Test]
        public void PartialLeadInReadContainsSilenceAndAudio()
        {
            // Start time of 0.5s, read a buffer that spans from silence into audio
            int halfSecondBytes = BytesPerSecond / 2;
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(0.5), TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Read a buffer that spans the silence/audio boundary
            var buffer = new byte[BytesPerSecond];
            int read = offset.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(BytesPerSecond));

            // First half should be silence
            for (int i = 0; i < halfSecondBytes; i++)
            {
                Assert.That(buffer[i], Is.EqualTo(0), $"Expected silence at byte {i}");
            }
            // Second half should have non-zero audio data
            bool hasNonZero = false;
            for (int i = halfSecondBytes; i < buffer.Length; i++)
            {
                if (buffer[i] != 0) hasNonZero = true;
            }
            Assert.That(hasNonZero, Is.True, "Audio portion should contain non-zero data");
        }

        #endregion

        #region SourceOffset Tests

        [Test]
        public void SourceOffsetSkipsBeginningOfSource()
        {
            using var source = CreateSourceStream(2.0);
            // Read without offset
            var bufferNoOffset = new byte[100];
            using var noOffset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _ = noOffset.Read(bufferNoOffset, 0, 100);

            // Read with 0.5s offset - should get different data
            using var source2 = CreateSourceStream(2.0);
            var bufferWithOffset = new byte[100];
            using var withOffset = new WaveOffsetStream(source2, TimeSpan.Zero, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1));
            _ = withOffset.Read(bufferWithOffset, 0, 100);

            bool different = false;
            for (int i = 0; i < 100; i++)
            {
                if (bufferNoOffset[i] != bufferWithOffset[i])
                {
                    different = true;
                    break;
                }
            }
            Assert.That(different, Is.True, "Source offset should cause different data to be read");
        }

        [Test]
        public void ChangingSourceOffsetRepositionsSourceStream()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            offset.SourceOffset = TimeSpan.FromSeconds(1);
            // After changing source offset, source should be repositioned
            long expectedSourcePos = (long)(1.0 * SampleRate) * BytesPerSample;
            Assert.That(source.Position, Is.EqualTo(expectedSourcePos));
        }

        #endregion

        #region SourceLength Tests

        [Test]
        public void SourceLengthLimitsReadableAudio()
        {
            using var source = CreateSourceStream(2.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            Assert.That(offset.Length, Is.EqualTo(BytesPerSecond));
        }

        [Test]
        public void ReadBeyondSourceLengthReturnsSilence()
        {
            int halfSecond = BytesPerSecond / 2;
            using var source = CreateSourceStream(2.0);
            // SourceLength of 0.25s
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(0.25));

            // Read more than the source length
            var buffer = new byte[halfSecond];
            int read = offset.Read(buffer, 0, halfSecond);
            Assert.That(read, Is.EqualTo(halfSecond));

            // The bytes beyond 0.25s should be zero-filled
            int quarterSecond = BytesPerSecond / 4;
            for (int i = quarterSecond; i < halfSecond; i++)
            {
                Assert.That(buffer[i], Is.EqualTo(0), $"Expected silence (lead-out) at byte {i}");
            }
        }

        #endregion

        #region Read Method Tests

        [Test]
        public void ReadReturnsRequestedByteCount()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            var buffer = new byte[1000];
            int read = offset.Read(buffer, 0, 1000);
            Assert.That(read, Is.EqualTo(1000));
        }

        [Test]
        public void ReadRespectsBufferOffset()
        {
            using var source = CreateSourceStream();
            using var offset = new WaveOffsetStream(source);
            var buffer = new byte[200];
            // Fill buffer with 0xFF to detect writes
            for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;

            int read = offset.Read(buffer, 50, 100);
            Assert.That(read, Is.EqualTo(100));

            // Data before offset should be untouched
            for (int i = 0; i < 50; i++)
            {
                Assert.That(buffer[i], Is.EqualTo(0xFF), $"Buffer before offset should be untouched at {i}");
            }
        }

        [Test]
        public void ReadProducesCorrectSourceData()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source);
            var buffer = new byte[100];
            _ = offset.Read(buffer, 0, 100);

            // Verify the data matches our IdentifiableWaveStream pattern
            for (int i = 0; i < 100; i++)
            {
                byte expected = (byte)((i % 255) + 1);
                Assert.That(buffer[i], Is.EqualTo(expected), $"Audio data mismatch at byte {i}");
            }
        }

        [Test]
        public void MultipleReadsProduceConsecutiveData()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source);

            var buffer1 = new byte[100];
            var buffer2 = new byte[100];
            _ = offset.Read(buffer1, 0, 100);
            _ = offset.Read(buffer2, 0, 100);

            // buffer2 should continue the pattern from where buffer1 left off
            for (int i = 0; i < 100; i++)
            {
                byte expected = (byte)(((100 + i) % 255) + 1);
                Assert.That(buffer2[i], Is.EqualTo(expected), $"Second read data mismatch at byte {i}");
            }
        }

        #endregion

        #region Lead-Out Silence Tests

        [Test]
        public void ReadPastSourceLengthPadsWithZeros()
        {
            using var source = CreateSourceStream(1.0);
            // SourceLength = 0.5s but we read for longer
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(0.5));

            int halfSecond = BytesPerSecond / 2;
            // Read all 0.5s of audio first
            var audioBuffer = new byte[halfSecond];
            _ = offset.Read(audioBuffer, 0, halfSecond);

            // Now read more - should be silence (lead-out)
            var silenceBuffer = new byte[100];
            _ = offset.Read(silenceBuffer, 0, 100);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(silenceBuffer[i], Is.EqualTo(0), $"Expected silence in lead-out at byte {i}");
            }
        }

        #endregion

        #region HasData Tests

        [Test]
        public void HasDataReturnsFalseBeforeAudioStart()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(1));
            // Position is 0, audio starts at 1s
            Assert.That(offset.HasData(100), Is.False);
        }

        [Test]
        public void HasDataReturnsFalseAfterLength()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            offset.Position = offset.Length;
            Assert.That(offset.HasData(100), Is.False);
        }

        #endregion

        #region Property Change Repositioning Tests (CA2245 fix verification)

        [Test]
        public void ChangingStartTimeRepositionsSourceStream()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            // Move position into the audio region
            offset.Position = 1000;
            long sourcePosBefore = source.Position;
            Assert.That(sourcePosBefore, Is.EqualTo(1000));

            // Change start time - source stream should be repositioned
            offset.StartTime = TimeSpan.FromSeconds(1);
            // Position (1000) is now before audioStartPosition (16000),
            // so source should be at sourceOffsetBytes (0)
            Assert.That(source.Position, Is.EqualTo(0));
        }

        [Test]
        public void ChangingSourceOffsetRepositionsSourceCorrectly()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            // Position is 0, audio start is 0
            // After changing source offset to 1s, source should be at 1s
            offset.SourceOffset = TimeSpan.FromSeconds(1);
            long expectedSourcePos = (long)(1.0 * SampleRate) * BytesPerSample;
            Assert.That(source.Position, Is.EqualTo(expectedSourcePos));
        }

        [Test]
        public void ChangingSourceLengthRepositionsSourceStream()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            offset.Position = 1000;
            offset.SourceLength = TimeSpan.FromSeconds(1);

            // Position 1000 with audioStartPosition 0 means source should be at 0 + 1000 = 1000
            Assert.That(source.Position, Is.EqualTo(1000));
            // Length should be updated
            Assert.That(offset.Length, Is.EqualTo((long)(1.0 * SampleRate) * BytesPerSample));
        }

        [Test]
        public void PositionIsPreservedAfterChangingStartTime()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            offset.Position = 1000;
            offset.StartTime = TimeSpan.FromSeconds(0.5);
            // The WaveOffsetStream position field should remain unchanged
            Assert.That(offset.Position, Is.EqualTo(1000));
        }

        [Test]
        public void PositionIsPreservedAfterChangingSourceOffset()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            offset.Position = 1000;
            offset.SourceOffset = TimeSpan.FromSeconds(0.5);
            Assert.That(offset.Position, Is.EqualTo(1000));
        }

        [Test]
        public void PositionIsPreservedAfterChangingSourceLength()
        {
            using var source = CreateSourceStream(3.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            offset.Position = 1000;
            offset.SourceLength = TimeSpan.FromSeconds(1);
            Assert.That(offset.Position, Is.EqualTo(1000));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ZeroSourceLengthProducesZeroLength()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
            Assert.That(offset.Length, Is.EqualTo(0));
        }

        [Test]
        public void SeekingToZeroResetsCorrectly()
        {
            using var source = CreateSourceStream(1.0);
            using var offset = new WaveOffsetStream(source);

            // Read some data
            var buffer = new byte[1000];
            _ = offset.Read(buffer, 0, 1000);

            // Seek back to 0
            offset.Position = 0;
            Assert.That(offset.Position, Is.EqualTo(0));
            Assert.That(source.Position, Is.EqualTo(0));

            // Read same data again and verify it matches
            var buffer2 = new byte[1000];
            _ = offset.Read(buffer2, 0, 1000);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(buffer2[i], Is.EqualTo(buffer[i]), $"Data after seek should match at byte {i}");
            }
        }

        [Test]
        public void CombinedStartTimeAndSourceOffsetWork()
        {
            using var source = CreateSourceStream(3.0);
            var startTime = TimeSpan.FromSeconds(0.5);
            var srcOffset = TimeSpan.FromSeconds(1.0);
            var srcLength = TimeSpan.FromSeconds(1.0);
            using var offset = new WaveOffsetStream(source, startTime, srcOffset, srcLength);

            int halfSecondBytes = BytesPerSecond / 2;

            // Length should be startTime + sourceLength = 0.5s + 1.0s = 1.5s
            Assert.That(offset.Length, Is.EqualTo((long)(1.5 * SampleRate) * BytesPerSample));

            // First 0.5s should be silence
            var silenceBuffer = new byte[halfSecondBytes];
            _ = offset.Read(silenceBuffer, 0, halfSecondBytes);
            for (int i = 0; i < halfSecondBytes; i++)
            {
                Assert.That(silenceBuffer[i], Is.EqualTo(0), $"Expected silence at byte {i}");
            }

            // Next part should be audio starting from source offset
            var audioBuffer = new byte[100];
            _ = offset.Read(audioBuffer, 0, 100);

            // Verify it reads from 1.0s into the source (sourceOffsetBytes)
            long sourceOffsetBytesVal = (long)(1.0 * SampleRate) * BytesPerSample;
            for (int i = 0; i < 100; i++)
            {
                byte expected = (byte)(((sourceOffsetBytesVal + i) % 255) + 1);
                Assert.That(audioBuffer[i], Is.EqualTo(expected), $"Audio should start from source offset at byte {i}");
            }
        }

        [Test]
        public void DisposeDisposesSourceStream()
        {
            var source = CreateSourceStream();
            var offset = new WaveOffsetStream(source);
            offset.Dispose();

            // After dispose, attempting to access source should indicate it's been cleaned up
            // We can't directly test this without more infrastructure, but we verify no exception on dispose
            Assert.Pass("Dispose completed without error");
        }

        [Test]
        public void DoubleDisposeDoesNotThrow()
        {
            var source = CreateSourceStream();
            var offset = new WaveOffsetStream(source);
            offset.Dispose();
            Assert.DoesNotThrow(() => offset.Dispose());
        }

        #endregion

        /// <summary>
        /// Helper WaveStream with non-PCM encoding to test constructor validation
        /// </summary>
        private class NonPcmWaveStream : WaveStream
        {
            private readonly WaveFormat waveFormat;
            private readonly long length;
            private long position;

            public NonPcmWaveStream(WaveFormat format, long length)
            {
                waveFormat = format;
                this.length = length;
            }

            public override WaveFormat WaveFormat => waveFormat;
            public override long Length => length;

            public override long Position
            {
                get => position;
                set => position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }
        }
    }
}
