using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace NAudio.Extras;

/// <summary>
/// Used by AudioPlaybackEngine
/// </summary>
public class CachedSound
{
    /// <summary>
    /// Audio data
    /// </summary>
    public float[] AudioData { get; }

    /// <summary>
    /// Format of the audio
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Creates a new CachedSound from a file
    /// </summary>
    public CachedSound(string audioFileName)
    {
        using var audioFileReader = new AudioFileReader(audioFileName);
        WaveFormat = audioFileReader.WaveFormat;
        AudioData = ReadAllSamples(audioFileReader);
    }

    /// <summary>
    /// Creates a new CachedSound from a stream (e.g. an embedded resource or in-memory byte
    /// array). The stream must be readable and seekable; the caller retains ownership of it.
    /// </summary>
    public CachedSound(Stream audioStream)
    {
        using var audioFileReader = new AudioFileReader(audioStream);
        WaveFormat = audioFileReader.WaveFormat;
        AudioData = ReadAllSamples(audioFileReader);
    }

    /// <summary>
    /// Reads an entire AudioFileReader into a float array
    /// </summary>
    private static float[] ReadAllSamples(AudioFileReader audioFileReader)
    {
        // TODO: could add resampling in here if required
        var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
        var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
        int samplesRead;
        while ((samplesRead = audioFileReader.Read(readBuffer.AsSpan())) > 0)
        {
            wholeFile.AddRange(readBuffer.Take(samplesRead));
        }
        return wholeFile.ToArray();
    }
}
