using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace NAudio.Sampler;

/// <summary>
/// Loop points embedded in a sample file (e.g. a WAV <c>smpl</c> chunk), in
/// sample frames. <see cref="Start"/> is inclusive and <see cref="End"/> is
/// exclusive (one past the last looped frame), matching the engine's
/// <see cref="SampleData"/> convention — note the WAV <c>smpl</c> chunk
/// itself stores an <em>inclusive</em> end, converted on read.
/// </summary>
public readonly struct SampleLoop
{
    /// <summary>Creates loop points (start inclusive, end exclusive).</summary>
    public SampleLoop(int start, int end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Loop start frame (inclusive).</summary>
    public int Start { get; }

    /// <summary>Loop end frame (exclusive, one past the last looped frame).</summary>
    public int End { get; }
}

/// <summary>
/// Decodes an audio file (or any <see cref="ISampleProvider"/>) fully into
/// mono/stereo float channel buffers for the sampler — the in-memory,
/// random-access form the voice engine plays from. WAV is read directly
/// (including any loop points authored in its <c>smpl</c> chunk); other
/// formats decode through any provided <see cref="ISampleProvider"/>
/// (e.g. <c>NAudio.SoundFile.SoundFileReader</c> for FLAC/Ogg). Shared by the
/// SFZ sample loader and the single-sample instrument.
/// </summary>
public static class WaveSampleLoader
{
    /// <summary>
    /// Reads a WAV file into channel buffers: <paramref name="left"/> is the
    /// left/mono channel and <paramref name="right"/> is the right channel, or
    /// null for a mono file. Channels beyond the first two are ignored.
    /// Returns false if the file is missing or empty.
    /// </summary>
    public static bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate) =>
        TryLoad(path, out left, out right, out sampleRate, out _);

    /// <summary>
    /// Reads a WAV file into channel buffers (see
    /// <see cref="TryLoad(string, out float[], out float[], out int)"/>), also
    /// surfacing the first loop authored in the file's <c>smpl</c> chunk —
    /// real sample libraries commonly put loop points in the WAV rather than
    /// in the instrument definition. <paramref name="embeddedLoop"/> is null
    /// when the file has no (valid) <c>smpl</c> loop; malformed or truncated
    /// chunks are ignored rather than failing the load.
    /// </summary>
    public static bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
        out SampleLoop? embeddedLoop)
    {
        left = null;
        right = null;
        sampleRate = 0;
        embeddedLoop = null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

        using var reader = new WaveFileReader(path);
        embeddedLoop = ReadSmplLoop(reader.Chunks);
        return TryLoad(reader.ToSampleProvider(), out left, out right, out sampleRate);
    }

    /// <summary>
    /// Reads an entire sample provider into channel buffers (left/mono and an
    /// optional right channel), decoding it fully into memory. Returns false if
    /// it yields no samples.
    /// </summary>
    public static bool TryLoad(ISampleProvider source, out float[] left, out float[] right, out int sampleRate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        sampleRate = source.WaveFormat.SampleRate;
        int channels = source.WaveFormat.Channels;

        var interleaved = new List<float>();
        var buffer = new float[8192];
        int read;
        while ((read = source.Read(buffer.AsSpan(0, buffer.Length))) > 0)
            for (int i = 0; i < read; i++) interleaved.Add(buffer[i]);

        int frames = channels > 0 ? interleaved.Count / channels : 0;
        left = null;
        right = null;
        if (frames == 0) return false;

        left = ExtractChannel(interleaved, channels, 0, frames);
        if (channels >= 2) right = ExtractChannel(interleaved, channels, 1, frames);
        return true;
    }

    // The standard `smpl` chunk: a 36-byte header (manufacturer, product,
    // samplePeriod, midiUnityNote, midiPitchFraction, smpteFormat,
    // smpteOffset, numSampleLoops, samplerData), then 24 bytes per loop
    // (cuePointId, type, dwStart, dwEnd, fraction, playCount). dwStart/dwEnd
    // are sample-frame indices and dwEnd is INCLUSIVE — converted here to the
    // engine's exclusive end. Takes the first loop; anything truncated or
    // malformed is ignored (returns null) rather than failing the load.
    private static SampleLoop? ReadSmplLoop(WaveChunks chunks)
    {
        const int headerSize = 36;
        const int loopSize = 24;

        var chunk = chunks.Find("smpl");
        if (chunk == null) return null;

        byte[] data;
        try { data = chunks.GetData(chunk); }
        catch (Exception) { return null; } // chunk extends past the file — ignore
        if (data.Length < headerSize + loopSize) return null;

        uint loopCount = BitConverter.ToUInt32(data, 28); // numSampleLoops
        if (loopCount < 1) return null;

        uint start = BitConverter.ToUInt32(data, headerSize + 8);  // dwStart
        uint end = BitConverter.ToUInt32(data, headerSize + 12);   // dwEnd (inclusive)
        if (end < start || end >= int.MaxValue) return null;
        return new SampleLoop((int)start, (int)end + 1);
    }

    private static float[] ExtractChannel(List<float> interleaved, int channels, int channel, int frames)
    {
        var data = new float[frames];
        for (int f = 0; f < frames; f++) data[f] = interleaved[f * channels + channel];
        return data;
    }
}
