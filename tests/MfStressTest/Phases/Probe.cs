using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace MfStressTest.Phases;

/// <summary>
/// Two-step start-up phase:
/// 1. <b>Probe</b> — for each codec we know about, ask MF whether it has any
///    output media types. Filters out codecs missing on this Windows SKU
///    (FLAC/ALAC are absent on stripped editions).
/// 2. <b>Breadth</b> — one pass through the (codec × rate × channel × sink)
///    matrix. Each combo that successfully encodes + decodes + resamples gets
///    added to the working set the soak phase samples from. Combos that
///    fail with a managed MF exception are skipped (expected: not every
///    bitrate / format is supported on every machine).
/// </summary>
internal static class Probe
{
    public static List<Combo> Run(Options o, string tempDir)
    {
        Console.WriteLine("== Probe phase ==");
        // streamCapable=false skips the (codec, Stream) combo in the breadth matrix.
        // WMA stream-based decoding consistently returns MF_E_ASF_INVALIDDATA at high
        // volume in our testing - documented as a known issue (see README "Known
        // issues"). WMA file-based reading still gets exercised below. FLAC streaming
        // would need a TranscodeContainerType GUID we don't yet expose.
        // ALAC and OPUS are included for visibility but not exercised:
        //   - ALAC: Windows ships an encoder MFT but the MP4 sink rejects every
        //     codec-private layout we've tried (bare 24-byte ALACSpecificConfig and
        //     FFmpeg's 36-byte 'alac'-FullBox wrapper) with MF_E_SINK_HEADERS_NOT_FOUND.
        //     Microsoft's encoder is undocumented. See Tools/prompts/flac-opus-alac-fix.md.
        //   - OPUS: decoder MFT only; GetOutputMediaTypes returns 0.
        var probe = new[]
        {
            ("MP3",  AudioSubtypes.MFAudioFormat_MP3,       "mp3",  TranscodeContainerTypes.MFTranscodeContainerType_MP3,   true),
            ("WMA",  AudioSubtypes.MFAudioFormat_WMAudioV8, "wma",  TranscodeContainerTypes.MFTranscodeContainerType_ASF,   false),
            ("AAC",  AudioSubtypes.MFAudioFormat_AAC,       "mp4",  TranscodeContainerTypes.MFTranscodeContainerType_MPEG4, true),
            ("FLAC", AudioSubtypes.MFAudioFormat_FLAC,      "flac", Guid.Empty,                                              false),
            ("OPUS", AudioSubtypes.MFAudioFormat_Opus,      "opus", Guid.Empty,                                              false),
        };

        var available = new List<CodecSpec>();
        foreach (var (name, subtype, ext, container, streamCapable) in probe)
        {
            var types = MediaFoundationEncoder.GetOutputMediaTypes(subtype);
            try
            {
                int n = types.Length;
                Console.WriteLine($"  {name,-4}: {n,3} output media types {(n > 0 ? "available" : "(missing)")}");
                if (n > 0) available.Add(new CodecSpec(name, subtype, ext, container, streamCapable));
            }
            finally
            {
                foreach (var mt in types) mt.Dispose();
            }
        }
        Console.WriteLine();

        Console.WriteLine("== Breadth phase ==");
        int[] sampleRates = { 22050, 44100, 48000 };
        int[] channels = { 1, 2 };
        var sinks = new[] { Sink.File, Sink.Stream };

        var combos = new List<Combo>();
        int attempted = 0, succeeded = 0;
        var sw = Stopwatch.StartNew();

        foreach (var codec in available)
            foreach (var rate in sampleRates)
                foreach (var ch in channels)
                    foreach (var sink in sinks)
                    {
                        if (sink == Sink.Stream && !codec.StreamCapable) continue;
                        attempted++;
                        var combo = new Combo(codec, rate, ch, sink);
                        string? skipReason = null;
                        if (TryBreadthRound(tempDir, combo, out skipReason))
                        {
                            combos.Add(combo);
                            succeeded++;
                            if (o.Verbose) Console.WriteLine($"  OK   {combo}");
                        }
                        else if (o.Verbose) Console.WriteLine($"  skip {combo}: {skipReason}");
                    }

        Console.WriteLine($"  breadth: {succeeded}/{attempted} combos OK in {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine();
        return combos;
    }

    private static bool TryBreadthRound(string tempDir, Combo combo, out string? skipReason)
    {
        try
        {
            Watchdog.Beat("breadth-encode", 0, combo);
            var encoded = MfPrimitives.EncodeOne(tempDir, combo, durationSeconds: 1.5, frequency: 1000.0, useFloatInput: false);
            Watchdog.Beat("breadth-decode", 0, combo);
            MfPrimitives.DecodeOne(encoded, combo, repositionFraction: 0.0, alsoResample: true, targetRate: 22050);
            MfPrimitives.DisposeEncoded(encoded);
            skipReason = null;
            return true;
        }
        // Per-combo failures are expected (codec/bitrate/format not supported here);
        // skip but report the reason so silently-skipped classes (e.g. FLAC/ALAC
        // bps mismatch) are debuggable under --verbose.
        catch (InvalidOperationException ex) { skipReason = $"{ex.GetType().Name}: {ex.Message}"; return false; }
        catch (NAudio.MediaFoundation.MediaFoundationException ex) { skipReason = $"MediaFoundationException 0x{ex.HResult:X8}: {ex.Message}"; return false; }
        catch (COMException ex) { skipReason = $"COMException 0x{ex.HResult:X8}: {ex.Message}"; return false; }
        catch (ArgumentException ex) { skipReason = $"ArgumentException: {ex.Message}"; return false; }
    }
}
