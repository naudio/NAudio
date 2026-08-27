using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Core.Tests.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.Mp3;

/// <summary>
/// Regression tests for issue #1419: with a Xing/Info header present, every forward seek
/// past the scanned tail silently rewound to the start of the file. The lazy table of
/// contents was gated on <c>IsLengthExact</c>, which a Xing <c>Frames</c> field sets true
/// without a single frame having been scanned.
/// </summary>
/// <remarks>
/// Uses a synthetic in-memory MP3 and a frame-stamping decompressor rather than a real
/// codec — see the note on <see cref="Mp3FileReaderLazyTocTests"/>. The stamp makes the
/// frame a seek landed on directly assertable, which an all-silence decompressor cannot.
/// </remarks>
[TestFixture]
[Category("UnitTest")]
public class Mp3FileReaderSeekTests
{
    private const double DurationSeconds = 10;
    private const int BytesPerSample = 4; // 16-bit stereo

    private static byte[] Mp3Bytes(bool withInfoHeader) =>
        withInfoHeader
            ? SyntheticMp3.CreateBytesWithInfoHeader(DurationSeconds)
            : SyntheticMp3.CreateBytes(DurationSeconds);

    private static int AudioFrameCount => SyntheticMp3.FramesForSeconds(DurationSeconds);

    /// <summary>
    /// File offset of audio frame <paramref name="audioFrameIndex"/>. With an Info header the
    /// header itself occupies file frame 0, so the audio is shifted along by one frame.
    /// </summary>
    private static long ExpectedFileOffset(bool withInfoHeader, int audioFrameIndex) =>
        (long)(withInfoHeader ? audioFrameIndex + 1 : audioFrameIndex) * SyntheticMp3.FrameSize;

    private static Mp3FileReaderBase Open(byte[] mp3, out FrameStampingMp3FrameDecompressor decompressor,
        bool legacyByteArrayOnly = false)
    {
        FrameStampingMp3FrameDecompressor created = null;
        var reader = new Mp3FileReaderBase(new MemoryStream(mp3),
            fmt => created = new FrameStampingMp3FrameDecompressor(fmt, legacyByteArrayOnly));
        decompressor = created;
        return reader;
    }

    // Note: Mp3FileReaderBase treats two Position writes inside ScrubDetectionWindowMs (30 ms)
    // as an interactive scrub and deliberately returns silence until the drag settles. Tests
    // that need to assert on decoded content therefore use one seek per reader, or advance by
    // reading rather than by writing Position again.
    private static long ReadStampAt(Mp3FileReaderBase reader, long sampleTarget)
    {
        reader.Position = sampleTarget * BytesPerSample;
        var buf = new byte[BytesPerSample];
        int read = reader.Read(buf, 0, buf.Length);
        Assert.That(read, Is.EqualTo(buf.Length), "Seek target should still have data after it");
        return FrameStampingMp3FrameDecompressor.ReadStamp(buf);
    }

    // Audio frame 10 exactly on a frame boundary, then the same frame entered part-way
    // through, then one far past anything the constructor scanned.
    [TestCase(true, 10, 0)]
    [TestCase(false, 10, 0)]
    [TestCase(true, 10, 500)]
    [TestCase(false, 10, 500)]
    [TestCase(true, 200, 0)]
    [TestCase(false, 200, 0)]
    [TestCase(true, 200, 777)]
    [TestCase(false, 200, 777)]
    public void SeekForward_PastScannedTail_LandsOnTargetFrame(bool withInfoHeader, int audioFrame, int sampleWithinFrame)
    {
        using var reader = Open(Mp3Bytes(withInfoHeader), out _);
        long target = (long)audioFrame * SyntheticMp3.SamplesPerFrame + sampleWithinFrame;

        long stamp = ReadStampAt(reader, target);

        Assert.That(stamp, Is.EqualTo(ExpectedFileOffset(withInfoHeader, audioFrame)),
            $"Seeking to sample {target} should decode audio frame {audioFrame}, " +
            $"not the frame at file offset {stamp}");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SeekForward_ToEveryFrameBoundary_LandsOnThatFrame(bool withInfoHeader)
    {
        // Frame-boundary targets were the second defect: ExtendTableOfContentsTo stopped one
        // frame short when the target landed exactly on a boundary, so no TOC entry covered it.
        var mp3 = Mp3Bytes(withInfoHeader);
        for (int audioFrame = 1; audioFrame < AudioFrameCount; audioFrame += 37)
        {
            using var reader = Open(mp3, out _);
            long stamp = ReadStampAt(reader, (long)audioFrame * SyntheticMp3.SamplesPerFrame);
            Assert.That(stamp, Is.EqualTo(ExpectedFileOffset(withInfoHeader, audioFrame)),
                $"Boundary seek to audio frame {audioFrame} landed on file offset {stamp}");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SeekThenReadToEnd_DoesNotOverrunLength(bool withInfoHeader)
    {
        // The symptom reported in #1419: after seeking, CurrentTime ran past TotalTime because
        // the seek had quietly rewound and the whole file was decoded again from the start.
        using var reader = Open(Mp3Bytes(withInfoHeader), out _);
        await reader.EnsureExactLengthAsync();
        long length = reader.Length;
        long target = length / 2 / BytesPerSample * BytesPerSample;
        reader.Position = target;

        var buf = new byte[16384];
        long total = 0;
        int n;
        do
        {
            n = reader.Read(buf, 0, buf.Length);
            total += n;
        } while (n > 0);

        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(length - target),
                "Reading to EOF after a mid-file seek should yield exactly the remainder");
            Assert.That(reader.Position, Is.EqualTo(length));
            Assert.That(reader.CurrentTime, Is.LessThanOrEqualTo(reader.TotalTime),
                "CurrentTime must not run past TotalTime");
        });
    }

    [Test]
    public void InfoHeaderFrame_IsNeverDecodedAsAudio()
    {
        // The Info/Xing frame is a valid MPEG frame but carries no audio, and decoding starts
        // past it. Indexing it as TOC entry 0 shifted every seek one frame (~26 ms) early.
        var mp3 = Mp3Bytes(withInfoHeader: true);
        long firstAudioOffset = SyntheticMp3.FrameSize;

        using (var reader = Open(mp3, out var sequential))
        {
            var buf = new byte[BytesPerSample];
            _ = reader.Read(buf, 0, buf.Length);
            Assert.That(FrameStampingMp3FrameDecompressor.ReadStamp(buf), Is.EqualTo(firstAudioOffset),
                "A fresh sequential read should start at the first audio frame");
            Assert.That(sequential.FramesDecoded, Has.None.Zero,
                "The Info header frame at file offset 0 should never be handed to the decoder");
        }

        using (var reader = Open(mp3, out var seekToZero))
        {
            Assert.That(ReadStampAt(reader, 0), Is.EqualTo(firstAudioOffset),
                "Seeking to sample 0 should also land on the first audio frame");
            Assert.That(seekToZero.FramesDecoded, Has.None.Zero,
                "The Info header frame should not be decoded as a warm-up frame either");
        }

        using (var reader = Open(mp3, out _))
        {
            Assert.That(ReadStampAt(reader, SyntheticMp3.SamplesPerFrame),
                Is.EqualTo(firstAudioOffset + SyntheticMp3.FrameSize),
                "Sample 1152 is the start of the second audio frame");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SeekToEnd_ReturnsNoDataInsteadOfReplayingFromStart(bool withInfoHeader)
    {
        using var reader = Open(Mp3Bytes(withInfoHeader), out _);
        await reader.EnsureExactLengthAsync();
        reader.Position = reader.Length;

        var buf = new byte[16384];
        int n = reader.Read(buf, 0, buf.Length);

        Assert.That(n, Is.Zero, "Seeking to the end should yield no data, not restart playback");
        Assert.That(reader.Position, Is.EqualTo(reader.Length));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Seek_WorksThroughLegacyByteArrayOnlyDecompressor(bool withInfoHeader)
    {
        // The reporter's setup: NLayer's Mp3FrameDecompressor predates Span and reaches the
        // reader through the default-interface-method fallback. Seeking must behave identically.
        using var reader = Open(Mp3Bytes(withInfoHeader), out _, legacyByteArrayOnly: true);

        long stamp = ReadStampAt(reader, 200L * SyntheticMp3.SamplesPerFrame);

        Assert.That(stamp, Is.EqualTo(ExpectedFileOffset(withInfoHeader, 200)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SeekForward_ProducesSamePcmAsSeekBackwardToSamePoint(bool withInfoHeader)
    {
        var mp3 = Mp3Bytes(withInfoHeader);
        long target = 150L * SyntheticMp3.SamplesPerFrame + 64;

        // Forward: straight to the target from a freshly opened reader.
        using var forward = Open(mp3, out _);
        forward.Position = target * BytesPerSample;
        var forwardBuf = new byte[8192];
        int forwardRead = forward.Read(forwardBuf, 0, forwardBuf.Length);

        // Backward: read sequentially past the target, then come back to it. Advancing by
        // reading rather than by a second Position write keeps this out of scrub mode.
        using var backward = Open(mp3, out _);
        long readPast = 300L * SyntheticMp3.SamplesPerFrame * BytesPerSample;
        var skipBuf = new byte[8192];
        while (backward.Position < readPast && backward.Read(skipBuf, 0, skipBuf.Length) > 0)
        {
        }
        backward.Position = target * BytesPerSample;
        var backwardBuf = new byte[8192];
        int backwardRead = backward.Read(backwardBuf, 0, backwardBuf.Length);

        Assert.That(forwardRead, Is.EqualTo(backwardRead));
        Assert.That(forwardBuf, Is.EqualTo(backwardBuf),
            "Reaching a position by seeking forward or backward should decode the same frames");
    }
}
