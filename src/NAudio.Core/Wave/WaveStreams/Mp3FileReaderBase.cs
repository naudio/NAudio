using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    class Mp3Index
    {
        public long FilePosition { get; set; }
        public long SamplePosition { get; set; }
        public int SampleCount { get; set; }
        public int ByteCount { get; set; }
    }

    /// <summary>
    /// Class for reading from MP3 files
    /// </summary>
    public class Mp3FileReaderBase : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private Stream mp3Stream;
        private readonly long mp3DataLength;
        private readonly long dataStartPosition;
        
        /// <summary>
        /// The MP3 wave format (n.b. NOT the output format of this stream - see the WaveFormat property)
        /// </summary>
        public Mp3WaveFormat Mp3WaveFormat { get; private set; }

        private readonly XingHeader xingHeader;
        private readonly bool ownInputStream;

        private List<Mp3Index> tableOfContents;
        private int tocIndex;

        private long totalSamples;
        private bool isLengthExact;
        private long scannedToFilePosition;
        private long scannedToSamplePosition;
        private readonly int bytesPerSample;
        private readonly int bytesPerDecodedFrame;

        private IMp3FrameDecompressor decompressor;
        
        private readonly byte[] decompressBuffer;
        private int decompressBufferOffset;
        private int decompressLeftovers;
        private bool repositionedFlag;

        private long position; // decompressed data position tracker
        private long? pendingPosition; // queued reposition target; applied on the next Read
        private long lastRepositionTickCount = -1;
        private bool inScrubMode;

        // Two Position writes arriving within ScrubDetectionWindowMs are treated as the
        // start of an interactive scrub. While in scrub mode, Read returns silence until
        // SettleWindowMs has elapsed since the latest Position write — gives the user a
        // chance to release the slider before audio resumes. Hides the per-reposition
        // bit-reservoir warm-up artifact that's audible under low-latency outputs (WASAPI).
        // A single Position write applies immediately on the next Read — clicks stay snappy.
        private const int ScrubDetectionWindowMs = 30;
        private const int SettleWindowMs = 50;

        private readonly object repositionLock = new object();


        /// <summary>Supports opening a MP3 file</summary>
        /// <param name="mp3FileName">MP3 File name</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReaderBase(string mp3FileName, FrameDecompressorBuilder frameDecompressorBuilder)
            : this(File.OpenRead(mp3FileName), frameDecompressorBuilder, true)
        {
        }



        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream">The incoming stream containing MP3 data</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        public Mp3FileReaderBase(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder)
            : this(inputStream, frameDecompressorBuilder, false)
        {
            
        }

        /// <summary>
        /// Constructor that takes an input stream and a frame decompressor builder
        /// </summary>
        /// <param name="inputStream">Input stream</param>
        /// <param name="frameDecompressorBuilder">Factory method to build a frame decompressor</param>
        /// <param name="ownInputStream">Whether we own the stream and should dispose it</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected Mp3FileReaderBase(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder, bool ownInputStream)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (frameDecompressorBuilder == null) throw new ArgumentNullException(nameof(frameDecompressorBuilder));
            this.ownInputStream = ownInputStream;
            try
            {
                mp3Stream = inputStream;
                Id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);

                dataStartPosition = mp3Stream.Position;
                var firstFrame = Mp3Frame.LoadFromStream(mp3Stream);
                if (firstFrame == null)
                    throw new InvalidDataException("Invalid MP3 file - no MP3 Frames Detected");
                xingHeader = XingHeader.LoadXingHeader(firstFrame);
                // If the header exists, we can skip over it when decoding the rest of the file
                if (xingHeader != null) dataStartPosition = mp3Stream.Position;

                // workaround for a longstanding issue with some files failing to load
                // because they report a spurious sample rate change
                var secondFrame = Mp3Frame.LoadFromStream(mp3Stream);
                if (secondFrame != null &&
                    (secondFrame.SampleRate != firstFrame.SampleRate ||
                     secondFrame.ChannelMode != firstFrame.ChannelMode))
                {
                    // assume that the first frame was some kind of VBR/LAME header that we failed to recognise properly
                    dataStartPosition = secondFrame.FileOffset;
                    // forget about the first frame, the second one is the first one we really care about
                    firstFrame = secondFrame;
                }

                mp3DataLength = mp3Stream.Length - dataStartPosition;

                // try for an ID3v1 tag as well
                mp3Stream.Position = mp3Stream.Length - 128;
                byte[] tag = new byte[128];
                _ = mp3Stream.Read(tag, 0, 128);
                if (tag[0] == 'T' && tag[1] == 'A' && tag[2] == 'G')
                {
                    Id3v1Tag = tag;
                    mp3DataLength -= 128;
                }

                // Bitrate on Mp3WaveFormat is informational; ACM/DMO/MFT/NLayer decoders
                // read frame-by-frame and ignore it. Use the first frame's bitrate rather
                // than averaging — averaging required a full file scan.
                Mp3WaveFormat = new Mp3WaveFormat(firstFrame.SampleRate,
                    firstFrame.ChannelMode == ChannelMode.Mono ? 1 : 2, firstFrame.FrameLength, firstFrame.BitRate);

                SeedTableOfContents(firstFrame);
                EstimateTotalSamples(firstFrame);

                mp3Stream.Position = dataStartPosition;
                decompressor = frameDecompressorBuilder(Mp3WaveFormat);
                waveFormat = decompressor.OutputFormat;
                bytesPerSample = (decompressor.OutputFormat.BitsPerSample)/8*decompressor.OutputFormat.Channels;
                // no MP3 frames have more than 1152 samples in them
                bytesPerDecodedFrame = 1152 * bytesPerSample;
                // some MP3s I seem to get double
                decompressBuffer = new byte[bytesPerDecodedFrame * 2];
            }
            catch (Exception)
            {
                if (ownInputStream) inputStream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Function that can create an MP3 Frame decompressor
        /// </summary>
        /// <param name="mp3Format">A WaveFormat object describing the MP3 file format</param>
        /// <returns>An MP3 Frame decompressor</returns>
        public delegate IMp3FrameDecompressor FrameDecompressorBuilder(WaveFormat mp3Format);

        private void SeedTableOfContents(Mp3Frame firstFrame)
        {
            tableOfContents = new List<Mp3Index>();
            var index = new Mp3Index
            {
                FilePosition = firstFrame.FileOffset,
                SamplePosition = 0,
                SampleCount = firstFrame.SampleCount,
                ByteCount = firstFrame.FrameLength,
            };
            tableOfContents.Add(index);
            scannedToFilePosition = firstFrame.FileOffset + firstFrame.FrameLength;
            scannedToSamplePosition = firstFrame.SampleCount;
            tocIndex = 0;
        }

        private void EstimateTotalSamples(Mp3Frame firstFrame)
        {
            if (xingHeader != null && xingHeader.Frames > 0)
            {
                totalSamples = (long)xingHeader.Frames * firstFrame.SampleCount;
                isLengthExact = true;
            }
            else
            {
                // duration_seconds = mp3DataLength * 8 / firstFrame.BitRate
                // total_samples    = duration_seconds * sampleRate
                // Exact for CBR, approximate for headerless VBR.
                totalSamples = (long)((double)mp3DataLength * 8.0 * firstFrame.SampleRate / firstFrame.BitRate);
                isLengthExact = false;
            }
        }

        // Caller must hold repositionLock. Saves and restores mp3Stream.Position.
        // Scans frame headers (no PCM data) appending to TOC until scannedToSamplePosition >=
        // targetSamplePosition or EOF is reached. On EOF, isLengthExact is set true and
        // totalSamples is replaced with the exact frame-summed value.
        private void ExtendTableOfContentsTo(long targetSamplePosition, CancellationToken cancellationToken)
        {
            if (isLengthExact) return;
            if (scannedToSamplePosition >= targetSamplePosition) return;

            long savedPosition = mp3Stream.Position;
            try
            {
                mp3Stream.Position = scannedToFilePosition;
                while (scannedToSamplePosition < targetSamplePosition)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Mp3Frame frame;
                    try
                    {
                        frame = Mp3Frame.LoadFromStream(mp3Stream, readData: false);
                    }
                    catch (EndOfStreamException)
                    {
                        frame = null;
                    }
                    if (frame == null)
                    {
                        isLengthExact = true;
                        totalSamples = scannedToSamplePosition;
                        break;
                    }
                    ValidateFrameFormat(frame);
                    AppendIfNewFrame(frame);
                }
            }
            finally
            {
                mp3Stream.Position = savedPosition;
            }
        }

        // Append a TOC entry for a frame iff it falls past our scanned region.
        // Idempotent for frames already covered (returns without appending).
        private void AppendIfNewFrame(Mp3Frame frame)
        {
            if (frame.FileOffset < scannedToFilePosition) return;
            var entry = new Mp3Index
            {
                FilePosition = frame.FileOffset,
                SamplePosition = scannedToSamplePosition,
                SampleCount = frame.SampleCount,
                ByteCount = (int)(mp3Stream.Position - frame.FileOffset),
            };
            tableOfContents.Add(entry);
            scannedToFilePosition = mp3Stream.Position;
            scannedToSamplePosition += frame.SampleCount;
        }

        private void ValidateFrameFormat(Mp3Frame frame)
        {
            if (frame.SampleRate != Mp3WaveFormat.SampleRate)
            {
                string message =
                    String.Format(
                        "Got a frame at sample rate {0}, in an MP3 with sample rate {1}. Mp3FileReader does not support sample rate changes.",
                        frame.SampleRate, Mp3WaveFormat.SampleRate);
                throw new InvalidOperationException(message);
            }
            int channels = frame.ChannelMode == ChannelMode.Mono ? 1 : 2;
            if (channels != Mp3WaveFormat.Channels)
            {
                string message =
                    String.Format(
                        "Got a frame with channel mode {0}, in an MP3 with {1} channels. Mp3FileReader does not support changes to channel count.",
                        frame.ChannelMode, Mp3WaveFormat.Channels);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// ID3v2 tag if present
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public Id3v2Tag Id3v2Tag { get; }

        /// <summary>
        /// ID3v1 tag if present
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public byte[] Id3v1Tag { get; }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        public Mp3Frame ReadNextFrame()
        {
            lock (repositionLock)
            {
                ApplyPendingReposition();
                var frame = ReadNextFrame(true);
                if (frame != null) position += frame.SampleCount * bytesPerSample;
                return frame;
            }
        }

        /// <summary>
        /// Reads the next mp3 frame
        /// </summary>
        /// <returns>Next mp3 frame, or null if EOF</returns>
        private Mp3Frame ReadNextFrame(bool readData)
        {
            Mp3Frame frame = null;
            try
            {
                frame = Mp3Frame.LoadFromStream(mp3Stream, readData);
                if (frame != null)
                {
                    AppendIfNewFrame(frame);
                    tocIndex++;
                }
                else if (!isLengthExact)
                {
                    // EOF reached during sequential read — we now know the exact length.
                    isLengthExact = true;
                    totalSamples = scannedToSamplePosition;
                }
            }
            catch (EndOfStreamException)
            {
                // suppress for now - it means we unexpectedly got to the end of the stream
                // half way through
            }
            return frame;
        }

        /// <summary>
        /// This is the length in bytes of data available to be read out from the Read method
        /// (i.e. the decompressed MP3 length).
        /// </summary>
        /// <remarks>
        /// Length is exact when the MP3 has a Xing/Info <c>Frames</c> field (most VBR encoders
        /// write one), or for CBR files (computed from the first frame's bitrate). For headerless
        /// VBR files Length is an estimate until enough of the file has been read sequentially or
        /// <see cref="EnsureExactLengthAsync"/> has run. Check <see cref="IsLengthExact"/> to
        /// disambiguate.
        /// </remarks>
        public override long Length => totalSamples * bytesPerSample;

        /// <summary>
        /// Returns <c>true</c> if <see cref="Length"/> reflects the exact frame-summed sample
        /// count, <c>false</c> if it is an estimate from the first frame's bitrate. False is
        /// only possible for headerless VBR files; CBR files and VBR files with a Xing/Info
        /// header report <c>true</c> immediately after the constructor returns.
        /// </summary>
        public bool IsLengthExact => isLengthExact;

        /// <summary>
        /// Forces a full scan of the MP3 frame index so <see cref="Length"/> reports the exact
        /// frame-summed sample count and <see cref="IsLengthExact"/> returns <c>true</c>.
        /// No-op (returns a completed task) if Length is already exact. Safe to call concurrently
        /// with Read and <see cref="Position"/> changes; scan is serialised against playback by
        /// the same internal lock. Restores the current playback position when complete.
        /// </summary>
        /// <param name="cancellationToken">Token observed between frames during the scan.</param>
        public Task EnsureExactLengthAsync(CancellationToken cancellationToken = default)
        {
            if (isLengthExact) return Task.CompletedTask;
            return Task.Run(() =>
            {
                lock (repositionLock)
                {
                    if (isLengthExact) return;
                    ExtendTableOfContentsTo(long.MaxValue, cancellationToken);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                // Queue the reposition; the audio thread applies it on its next Read.
                // Decouples the UI thread from stream I/O / TOC extension / decoder reset,
                // and naturally coalesces rapid scrub events: only the latest queued target
                // is acted on. The getter returns the just-set value so UI scrub timers
                // don't fight the user's drag.
                lock (repositionLock)
                {
                    value = Math.Max(Math.Min(value, Length), 0);
                    var now = Environment.TickCount64;
                    if (lastRepositionTickCount >= 0 && now - lastRepositionTickCount < ScrubDetectionWindowMs)
                    {
                        inScrubMode = true;
                    }
                    pendingPosition = value;
                    position = value;
                    lastRepositionTickCount = now;
                }
            }
        }

        // Caller must hold repositionLock. Drains any queued Position change: extends the
        // TOC if the target is past the scanned tail, looks up the target frame, seeks the
        // input stream, and primes the decompress-buffer offset for sub-frame seek. The
        // actual decoder.Reset + warm-up frames happen in Read via repositionedFlag.
        private void ApplyPendingReposition()
        {
            if (pendingPosition is not long target) return;
            pendingPosition = null;

            // Re-clamp: Length may have changed since the setter was called (e.g. concurrent
            // EnsureExactLengthAsync, or our own ExtendTableOfContentsTo below shrinking the
            // estimate to the real EOF).
            target = Math.Max(Math.Min(target, Length), 0);
            var samplePosition = target / bytesPerSample;

            if (samplePosition > scannedToSamplePosition && !isLengthExact)
            {
                ExtendTableOfContentsTo(samplePosition, CancellationToken.None);
                target = Math.Max(Math.Min(target, Length), 0);
                samplePosition = target / bytesPerSample;
            }

            Mp3Index mp3Index = null;
            for (int index = 0; index < tableOfContents.Count; index++)
            {
                if (tableOfContents[index].SamplePosition + tableOfContents[index].SampleCount > samplePosition)
                {
                    mp3Index = tableOfContents[index];
                    tocIndex = index;
                    break;
                }
            }

            decompressBufferOffset = 0;
            decompressLeftovers = 0;
            repositionedFlag = true;

            if (mp3Index != null)
            {
                mp3Stream.Position = mp3Index.FilePosition;
                var frameOffset = samplePosition - mp3Index.SamplePosition;
                if (frameOffset > 0)
                {
                    decompressBufferOffset = (int)frameOffset * bytesPerSample;
                }
            }
            else
            {
                mp3Stream.Position = mp3DataLength + dataStartPosition;
            }

            position = target;
        }

        /// <summary>
        /// Reads decompressed PCM data from our MP3 file.
        /// </summary>
        public override int Read(Span<byte> sampleBuffer)
        {
            int numBytes = sampleBuffer.Length;
            int bytesRead = 0;
            lock (repositionLock)
            {
                if (pendingPosition.HasValue && inScrubMode)
                {
                    // While scrubbing rapidly, hold silence until repositions stop for
                    // SettleWindowMs. Avoids emitting a fragment-of-audio-with-warm-up-click
                    // for every mouse-move event under low-latency output.
                    var elapsed = Environment.TickCount64 - lastRepositionTickCount;
                    if (elapsed < SettleWindowMs)
                    {
                        sampleBuffer.Clear();
                        return numBytes;
                    }
                    inScrubMode = false;
                }
                ApplyPendingReposition();
                if (decompressLeftovers != 0)
                {
                    int toCopy = Math.Min(decompressLeftovers, numBytes);
                    decompressBuffer.AsSpan(decompressBufferOffset, toCopy).CopyTo(sampleBuffer);
                    decompressLeftovers -= toCopy;
                    if (decompressLeftovers == 0)
                    {
                        decompressBufferOffset = 0;
                    }
                    else
                    {
                        decompressBufferOffset += toCopy;
                    }
                    bytesRead += toCopy;
                }

                int targetTocIndex = tocIndex; // the frame index that contains the requested data

                if (repositionedFlag)
                {
                    decompressor.Reset();

                    // Seek back a few frames of the stream to get the reset decoder decode a few
                    // warm-up frames before reading the requested data. Without the warm-up phase,
                    // the first half of the frame after the reset is attenuated and does not resemble
                    // the data as it would be when reading sequentially from the beginning, because
                    // the decoder is missing the required overlap from the previous frame.
                    tocIndex = Math.Max(0, tocIndex - 3); // no warm-up at the beginning of the stream
                    mp3Stream.Position = tableOfContents[tocIndex].FilePosition;

                    repositionedFlag = false;
                }

                while (bytesRead < numBytes)
                {
                    Mp3Frame frame = ReadNextFrame(true); // internal read - should not advance position
                    if (frame != null)
                    {
                        int decompressed = decompressor.DecompressFrame(frame, decompressBuffer.AsSpan());

                        if (tocIndex <= targetTocIndex || decompressed == 0)
                        {
                            // The first frame after a reset usually does not immediately yield decoded samples.
                            // Because the next instructions will fail if a buffer offset is set and the frame
                            // decoding didn't return data, we skip the part.
                            // We skip the following instructions also after decoding a warm-up frame.
                            continue;
                        }
                        // Two special cases can happen here:
                        // 1. We are interested in the first frame of the stream, but need to read the second frame too
                        //    for the decoder to return decoded data
                        // 2. We are interested in the second frame of the stream, but because reading the first frame
                        //    as warm-up didn't yield any data (because the decoder needs two frames to return data), we
                        //    get data from the first and second frame.
                        //    This case needs special handling, and we have to purge the data of the first frame.
                        else if (tocIndex == targetTocIndex + 1 && decompressed == bytesPerDecodedFrame * 2)
                        {
                            // Purge the first frame's data
                            Array.Copy(decompressBuffer, bytesPerDecodedFrame, decompressBuffer, 0, bytesPerDecodedFrame);
                            decompressed = bytesPerDecodedFrame;
                        }

                        int toCopy = Math.Min(decompressed - decompressBufferOffset, numBytes - bytesRead);
                        decompressBuffer.AsSpan(decompressBufferOffset, toCopy).CopyTo(sampleBuffer.Slice(bytesRead));
                        if ((toCopy + decompressBufferOffset) < decompressed)
                        {
                            decompressBufferOffset = toCopy + decompressBufferOffset;
                            decompressLeftovers = decompressed - decompressBufferOffset;
                        }
                        else
                        {
                            // no lefovers
                            decompressBufferOffset = 0;
                            decompressLeftovers = 0;
                        }
                        bytesRead += toCopy;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            Debug.Assert(bytesRead <= numBytes, "MP3 File Reader read too much");
            position += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Reads decompressed PCM data from our MP3 file.
        /// </summary>
        public override int Read(byte[] sampleBuffer, int offset, int numBytes)
            => Read(sampleBuffer.AsSpan(offset, numBytes));

        /// <summary>
        /// Xing header if present
        /// </summary>
        public XingHeader XingHeader => xingHeader;

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mp3Stream != null)
                {
                    if (ownInputStream)
                    {
                        mp3Stream.Dispose();
                    }
                    mp3Stream = null;
                }
                if (decompressor != null)
                {
                    decompressor.Dispose();
                    decompressor = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
