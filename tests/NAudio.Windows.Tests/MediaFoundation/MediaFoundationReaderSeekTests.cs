using System;
using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Windows.Tests.MediaFoundation;

/// <summary>
/// Regression tests for the inexact-seek fix in <see cref="MediaFoundationReader"/> (issue #628).
/// IMFSourceReader.SetCurrentPosition only seeks to the nearest container keyframe at or before the
/// requested time, so the reader has to read forward and skip into the decoded buffer to land on the
/// exact requested position. These tests decode through Media Foundation but need no audio hardware,
/// so they are intentionally <b>not</b> marked IntegrationTest and run in headless CI.
/// </summary>
[TestFixture]
public class MediaFoundationReaderSeekTests
{
    [SetUp]
    public void SetUp()
    {
        MediaFoundationApi.Startup();
    }

    /// <summary>
    /// For a lossless source (PCM WAV) the bytes returned after a seek must be identical to the bytes
    /// read sequentially up to the same position, and the reported Position must stay consistent.
    /// </summary>
    [Test]
    public void SeekReadMatchesSequentialReadForLosslessSource()
    {
        var path = CreateTempSineWav(seconds: 4);
        try
        {
            int blockAlign;
            long target;
            int count;
            byte[] expected;

            using (var reference = new MediaFoundationReader(path))
            {
                blockAlign = reference.WaveFormat.BlockAlign;
                // a block-aligned position roughly two seconds in
                target = reference.WaveFormat.AverageBytesPerSecond * 2L;
                target -= target % blockAlign;
                count = reference.WaveFormat.AverageBytesPerSecond / 4; // 0.25s

                // read sequentially from the start: discard everything up to target, then capture count bytes
                var scratch = new byte[(int)target];
                Assert.That(ReadFull(reference, scratch, scratch.Length), Is.EqualTo(scratch.Length));
                expected = new byte[count];
                Assert.That(ReadFull(reference, expected, count), Is.EqualTo(count));
            }

            using var seeker = new MediaFoundationReader(path);
            seeker.Position = target;
            var actual = new byte[count];
            int got = ReadFull(seeker, actual, count);

            Assert.That(got, Is.EqualTo(count));
            Assert.That(actual, Is.EqualTo(expected),
                "PCM read after seeking differs from the bytes read sequentially to the same position");
            Assert.That(seeker.Position, Is.EqualTo(target + got),
                "Position accounting drifted after a seek");
        }
        finally
        {
            TryDelete(path);
        }
    }

    /// <summary>
    /// For a compressed source (MP3) where SetCurrentPosition is inexact, the decoded audio after a
    /// seek must line up with the same point in a full sequential decode. Before the fix the reader
    /// returned audio from the preceding frame boundary, shifting the alignment by up to a whole MP3
    /// frame (~1152 samples). MP3 is lossy so we measure alignment by best-fit lag rather than bytes.
    /// </summary>
    [Test]
    public void SeekAlignsDecodedAudioForCompressedSource()
    {
        // only run when an MP3 encoder is actually available on this machine
        var mp3MediaType = MediaFoundationEncoder.SelectMediaType(
            AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(44100, 16, 1), 0);
        if (mp3MediaType == null) Assert.Ignore("No MP3 encoder available");

        var path = Path.Combine(Path.GetTempPath(), $"naudio_mf_seek_{Guid.NewGuid():N}.mp3");
        try
        {
            var signal = new SignalGenerator(44100, 1)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = 440,
                Gain = 0.5
            }.Take(TimeSpan.FromSeconds(4));
            MediaFoundationEncoder.EncodeToMp3(signal.ToWaveProvider(), path, 128000);

            int blockAlign, sampleRate;
            short[] reference;
            using (var refReader = new MediaFoundationReader(path))
            {
                blockAlign = refReader.WaveFormat.BlockAlign;
                sampleRate = refReader.WaveFormat.SampleRate;
                if (refReader.WaveFormat.Channels != 1) Assert.Ignore("Expected a mono MP3 decode");
                reference = ReadAllSamples(refReader);
            }

            const int window = 8192;
            const int maxLag = 2000;
            int targetSample = sampleRate * 2; // ~2s in
            if (reference.Length < targetSample + window + maxLag)
                Assert.Ignore("Decoded MP3 was shorter than expected");

            long targetByte = (long)targetSample * blockAlign;
            using var seeker = new MediaFoundationReader(path);
            seeker.Position = targetByte;
            var raw = new byte[window * blockAlign];
            Assert.That(ReadFull(seeker, raw, raw.Length), Is.EqualTo(raw.Length),
                "could not read a full window after seeking");
            var win = new short[window];
            Buffer.BlockCopy(raw, 0, win, 0, raw.Length);

            // find the lag (in samples) that best aligns the post-seek window with the reference decode
            long bestSum = long.MaxValue;
            int bestLag = int.MaxValue;
            for (int lag = -maxLag; lag <= maxLag; lag++)
            {
                long sum = 0;
                int baseIdx = targetSample + lag;
                for (int i = 0; i < window; i++)
                {
                    long d = win[i] - reference[baseIdx + i];
                    sum += d * d;
                    if (sum >= bestSum) break; // can't beat the current best, abandon early
                }
                if (sum < bestSum)
                {
                    bestSum = sum;
                    bestLag = lag;
                }
            }

            Assert.That(Math.Abs(bestLag), Is.LessThan(256),
                $"audio after seeking was misaligned by {bestLag} samples from the requested position");
        }
        finally
        {
            TryDelete(path);
        }
    }

    /// <summary>
    /// After a seek on a compressed source the reported Position must stay on a frame boundary.
    /// The decoder buffer's start was derived by truncating its 100ns timestamp to bytes, which can
    /// land mid-frame, so Position ended up a byte or two off and every later read inherited it.
    /// </summary>
    [TestCase(AudioSubtypesName.Mp3, 1000)]
    [TestCase(AudioSubtypesName.Mp3, 0)]
    [TestCase(AudioSubtypesName.Aac, 1000)]
    [TestCase(AudioSubtypesName.Aac, 0)]
    public void SeekKeepsPositionFrameAlignedForCompressedSource(AudioSubtypesName codec, int targetOffset)
    {
        var subtype = codec == AudioSubtypesName.Mp3 ? AudioSubtypes.MFAudioFormat_MP3 : AudioSubtypes.MFAudioFormat_AAC;
        var inputFormat = new WaveFormat(44100, 16, 2);
        if (MediaFoundationEncoder.SelectMediaType(subtype, inputFormat, 0) == null)
            Assert.Ignore($"No {codec} encoder available");

        var path = Path.Combine(Path.GetTempPath(), $"naudio_mf_seek_{Guid.NewGuid():N}.{(codec == AudioSubtypesName.Mp3 ? "mp3" : "m4a")}");
        try
        {
            var signal = new SignalGenerator(44100, 2) { Frequency = 440, Gain = 0.5 }.Take(TimeSpan.FromSeconds(3));
            using (var encoder = new MediaFoundationEncoder(MediaFoundationEncoder.SelectMediaType(subtype, inputFormat, 128000)!))
                encoder.Encode(path, signal.ToWaveProvider16());

            using var reader = new MediaFoundationReader(path,
                new MediaFoundationReader.MediaFoundationReaderSettings { RepositionInRead = false });
            var blockAlign = reader.WaveFormat.BlockAlign;
            Assert.That(reader.Length % blockAlign, Is.Zero, $"Length {reader.Length} is not a whole number of frames");
            // an unaligned request part way in; it should be rounded down to a frame boundary
            var target = reader.Length / 2 + targetOffset + 1;
            reader.Position = target;
            Assert.That(reader.Position % blockAlign, Is.Zero, $"Position {reader.Position} is not frame aligned after seek");
            Assert.That(reader.Position, Is.EqualTo(target - target % blockAlign), "seek did not land on the requested frame");

            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond / 2];
            var before = reader.Position;
            var read = ReadFull(reader, buffer, buffer.Length);
            Assert.That(reader.Position, Is.EqualTo(before + read));
            Assert.That(reader.Position % blockAlign, Is.Zero, "Position is not frame aligned after reading");
        }
        finally
        {
            TryDelete(path);
        }
    }

    public enum AudioSubtypesName { Mp3, Aac }

    private static string CreateTempSineWav(int seconds)
    {
        var path = Path.Combine(Path.GetTempPath(), $"naudio_mf_seek_{Guid.NewGuid():N}.wav");
        var signal = new SignalGenerator(44100, 1)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = 440,
            Gain = 0.5
        }.Take(TimeSpan.FromSeconds(seconds));
        WaveFileWriter.CreateWaveFile16(path, signal);
        return path;
    }

    private static int ReadFull(WaveStream reader, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = reader.Read(buffer, total, count - total);
            if (read == 0) break;
            total += read;
        }
        return total;
    }

    private static short[] ReadAllSamples(WaveStream reader)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            ms.Write(buffer, 0, read);
        }
        var bytes = ms.ToArray();
        var samples = new short[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, samples, 0, samples.Length * 2);
        return samples;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // best-effort temp cleanup
        }
    }
}
