using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MfStressTest;

/// <summary>One supported codec on this machine, with the metadata each phase needs.</summary>
internal sealed record CodecSpec(string Name, Guid Subtype, string Extension, Guid StreamContainer, bool StreamCapable);

/// <summary>One concrete encode/decode test scenario: codec × rate × channels × file-or-stream.</summary>
internal sealed record Combo(CodecSpec Codec, int SampleRate, int Channels, Sink Sink)
{
    public override string ToString() => $"{Codec.Name} {SampleRate}Hz {Channels}ch {Sink}";
}

/// <summary>Result of an <see cref="MfPrimitives.EncodeOne"/> — exactly one of File or Stream is set.</summary>
internal sealed class EncodedClip
{
    public string? File { get; }
    public MemoryStream? Stream { get; }
    public Combo Combo { get; }
    public EncodedClip(string? file, MemoryStream? stream, Combo combo)
    {
        File = file; Stream = stream; Combo = combo;
    }
}

/// <summary>
/// Shared encode/decode/resample/drain primitives used by every phase.
/// Nothing in here knows about the watchdog, CLI, or run mode - phases compose
/// these primitives into a scenario.
///
/// To add a new test scenario: add a new file under Phases/ that calls these
/// primitives and beats the watchdog at the appropriate granularity.
/// </summary>
internal static class MfPrimitives
{
    /// <summary>Generates a SignalGenerator-backed PCM source. Float input is rare but supported by some encoders.</summary>
    public static IWaveProvider MakeSignal(int sampleRate, int channels, double durationSeconds, double frequency, bool useFloatInput)
    {
        var sg = new SignalGenerator(sampleRate, channels) { Frequency = frequency, Gain = 0.25 }
            .Take(TimeSpan.FromSeconds(durationSeconds));
        return useFloatInput ? sg.ToWaveProvider() : sg.ToWaveProvider16();
    }

    /// <summary>Encode a synthetic signal to file or MemoryStream per the combo's <see cref="Sink"/>.</summary>
    public static EncodedClip EncodeOne(string tempDir, Combo combo, double durationSeconds, double frequency, bool useFloatInput)
    {
        var pcm = MakeSignal(combo.SampleRate, combo.Channels, durationSeconds, frequency, useFloatInput);
        int bitRate = combo.Codec.Name switch
        {
            "MP3"  => combo.Channels == 1 ? 64_000  : 128_000,
            "WMA"  => combo.Channels == 1 ? 64_000  : 128_000,
            "AAC"  => combo.Channels == 1 ? 96_000  : 128_000,
            "FLAC" => 0, // lossless
            "ALAC" => 0, // lossless
            _      => 128_000,
        };

        if (combo.Sink == Sink.File)
        {
            string path = Path.Combine(tempDir, $"clip_{Guid.NewGuid():N}.{combo.Codec.Extension}");
            EncodeToFile(combo.Codec, pcm, path, bitRate);
            return new EncodedClip(path, null, combo);
        }
        else
        {
            var ms = new MemoryStream();
            EncodeToStream(combo.Codec, pcm, ms, bitRate);
            ms.Position = 0;
            return new EncodedClip(null, ms, combo);
        }
    }

    static void EncodeToFile(CodecSpec codec, IWaveProvider pcm, string path, int bitRate)
    {
        if (codec.Name == "MP3")        MediaFoundationEncoder.EncodeToMp3(pcm, path, bitRate);
        else if (codec.Name == "WMA")   MediaFoundationEncoder.EncodeToWma(pcm, path, bitRate);
        else if (codec.Name == "AAC")   MediaFoundationEncoder.EncodeToAac(pcm, path, bitRate);
        else                            EncodeRaw(codec, pcm, path);
    }

    static void EncodeToStream(CodecSpec codec, IWaveProvider pcm, Stream stream, int bitRate)
    {
        if (codec.Name == "MP3")        MediaFoundationEncoder.EncodeToMp3(pcm, stream, bitRate);
        else if (codec.Name == "WMA")   MediaFoundationEncoder.EncodeToWma(pcm, stream, bitRate);
        else if (codec.Name == "AAC")   MediaFoundationEncoder.EncodeToAac(pcm, stream, bitRate);
        else throw new InvalidOperationException($"stream encoding not wired for {codec.Name}");
    }

    static void EncodeRaw(CodecSpec codec, IWaveProvider pcm, string path)
    {
        // FLAC / ALAC: raw Encode path. Lossless, so bit rate is irrelevant - pick the
        // first matching media type for the input format.
        var allTypes = MediaFoundationEncoder.GetOutputMediaTypes(codec.Subtype);
        MediaType? selected = null;
        try
        {
            selected = allTypes.FirstOrDefault(mt =>
                mt.SampleRate == pcm.WaveFormat.SampleRate && mt.ChannelCount == pcm.WaveFormat.Channels);
            if (selected == null) throw new InvalidOperationException($"no {codec.Name} media type for {pcm.WaveFormat}");
            foreach (var mt in allTypes) if (!ReferenceEquals(mt, selected)) mt.Dispose();
            using var encoder = new MediaFoundationEncoder(selected);
            encoder.Encode(path, pcm);
        }
        catch
        {
            foreach (var mt in allTypes) if (!ReferenceEquals(mt, selected)) mt.Dispose();
            selected?.Dispose();
            throw;
        }
        selected?.Dispose();
    }

    /// <summary>
    /// Decode the encoded clip back to PCM, optionally repositioning mid-stream and
    /// optionally resampling. Beats the watchdog inside the Read loop so a hung
    /// <c>ReadSample</c> will fire the watchdog within its timeout.
    /// </summary>
    public static void DecodeOne(EncodedClip clip, Combo combo, double repositionFraction, bool alsoResample, int targetRate)
    {
        // 50/50 between file-based and stream-based reader (only stream when the source is a stream).
        bool useStream = clip.Stream != null && (clip.File == null || (Random.Shared.Next(2) == 0));

        using WaveStream reader = useStream
            ? new StreamMediaFoundationReader(clip.Stream!)
            : new MediaFoundationReader(clip.File!);

        DrainAndReposition(reader, repositionFraction);

        if (alsoResample && reader.WaveFormat.SampleRate != targetRate)
        {
            // Re-create reader for the resampler (the previous one is at EOF).
            if (useStream) clip.Stream!.Position = 0;
            using WaveStream r2 = useStream
                ? new StreamMediaFoundationReader(clip.Stream!)
                : new MediaFoundationReader(clip.File!);
            using var resampler = new MediaFoundationResampler(r2, targetRate) { ResamplerQuality = 60 };
            Drain(resampler);
        }
    }

    /// <summary>Drain to EOF; if requested, reposition mid-stream and continue draining.</summary>
    public static void DrainAndReposition(WaveStream reader, double repositionFraction)
    {
        long total = reader.Length;
        Span<byte> buf = stackalloc byte[8192];
        long limit = (repositionFraction > 0 && total > 0) ? (long)(total * repositionFraction) : total;
        long read = 0;
        while (read < limit)
        {
            Watchdog.Beat();
            int got = reader.Read(buf);
            if (got == 0) break;
            read += got;
        }
        if (repositionFraction > 0 && total > 0)
        {
            Watchdog.Beat();
            // Snap to a sample-frame boundary and back off ~8 frames from EOF so we don't
            // land in the priming/encoder-delay tail where MF returns MF_E_INVALID_POSITION.
            long blockAlign = Math.Max(1, reader.WaveFormat.BlockAlign);
            long target = (long)(total * (1 - repositionFraction));
            target -= target % blockAlign;
            long maxTarget = total - 8 * blockAlign;
            if (maxTarget < 0) maxTarget = 0;
            if (target > maxTarget) target = maxTarget;
            try
            {
                reader.Position = target;
            }
            catch (NAudio.MediaFoundation.MediaFoundationException ex) when (ex.HResult == unchecked((int)0xC00D36E5))
            {
                // MF_E_INVALID_POSITION: very short clip whose seekable range doesn't
                // include our target. Drop reposition silently and tally the counter.
                Interlocked.Increment(ref Counters.InvalidPositionCount);
                return;
            }
            while (true)
            {
                Watchdog.Beat();
                int got = reader.Read(buf);
                if (got == 0) break;
            }
        }
    }

    /// <summary>Drain an arbitrary <see cref="IWaveProvider"/> to EOF, beating the watchdog per Read.</summary>
    public static void Drain(IWaveProvider provider)
    {
        Span<byte> buf = stackalloc byte[8192];
        while (true)
        {
            Watchdog.Beat();
            int got = provider.Read(buf);
            if (got == 0) break;
        }
    }

    /// <summary>Delete the file (if any) and dispose the stream (if any).</summary>
    public static void DisposeEncoded(EncodedClip clip)
    {
        if (clip.File != null)
        {
            try { File.Delete(clip.File); } catch { /* ignore */ }
        }
        clip.Stream?.Dispose();
    }

    /// <summary>
    /// Stress the S1 dual-finalizer pattern: enumerate transforms and media types,
    /// abandon the wrappers undisposed, let the finalizer thread release them in
    /// undefined order with the wrapper's own IntPtr release.
    /// </summary>
    public static void ChurnEnumerators()
    {
        foreach (var _ in MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioDecoder)) { }
        foreach (var _ in MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEncoder)) { }
        foreach (var _ in MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect)) { }
        var mts = MediaFoundationEncoder.GetOutputMediaTypes(AudioSubtypes.MFAudioFormat_MP3);
        foreach (var _ in mts) { }
    }
}
