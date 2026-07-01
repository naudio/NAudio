using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave.SampleProviders;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// AudioFileReader simplifies opening an audio file in NAudio
/// Simply pass in the filename, and it will attempt to open the
/// file and set up a conversion path that turns into PCM IEEE float.
/// ACM codecs will be used for conversion.
/// It provides a volume property and implements both WaveStream and
/// ISampleProvider, making it possibly the only stage in your audio
/// pipeline necessary for simple playback scenarios
/// </summary>
public class AudioFileReader : WaveStream, ISampleProvider
{
    private WaveStream readerStream; // the waveStream which we will use for all positioning
    private SampleChannel sampleChannel; // sample provider that gives us most stuff we need
    private int destBytesPerSample;
    private int sourceBytesPerSample;
    private long length;
    private readonly object lockObject;

    /// <summary>
    /// Initializes a new instance of AudioFileReader
    /// </summary>
    /// <param name="fileName">The file to open</param>
    public AudioFileReader(string fileName)
    {
        lockObject = new object();
        FileName = fileName;
        CreateReaderStream(fileName);
        Init();
    }

    /// <summary>
    /// Initializes a new instance of AudioFileReader from a stream. The audio format is
    /// detected from the stream contents, so no file name is required. WAV and AIFF streams
    /// are handled by the cross-platform readers; any other format is passed to Media
    /// Foundation (which requires the NAudio.Wasapi package and Windows).
    /// </summary>
    /// <param name="inputStream">The stream to read from. It must be readable and seekable, and
    /// the caller retains ownership of it — disposing the AudioFileReader does not dispose the
    /// underlying stream.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is unreadable or unseekable.</exception>
    public AudioFileReader(Stream inputStream)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        lockObject = new object();
        CreateReaderStream(inputStream);
        Init();
    }

    /// <summary>
    /// Sets up the sample channel and cached metrics once <see cref="readerStream"/> is created
    /// </summary>
    private void Init()
    {
        sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
        sampleChannel = new SampleChannel(readerStream, false);
        destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
        length = SourceToDest(readerStream.Length);
    }

    /// <summary>
    /// Creates the reader stream, supporting all filetypes in the core NAudio library,
    /// and ensuring we are in PCM format
    /// </summary>
    /// <param name="fileName">File Name</param>
    private void CreateReaderStream(string fileName)
    {
        if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            readerStream = new WaveFileReader(fileName);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
#if !WINDOWS
                throw new InvalidOperationException("WAV files with non-PCM encoding require Windows for ACM codec conversion");
#else
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
#endif
            }
        }
        else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
#if WASAPI
            if (Environment.OSVersion.Version.Major < 6)
                readerStream = new Mp3FileReader(fileName);
            else // make MediaFoundationReader the default for MP3 going forwards
                readerStream = new MediaFoundationReader(fileName);
#else
            throw new InvalidOperationException("MP3 file reading requires the NAudio.Wasapi package for Media Foundation codecs");
#endif
        }
        else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
        {
            readerStream = new AiffFileReader(fileName);
        }
        else
        {
#if WASAPI
            // fall back to media foundation reader, see if that can play it
            readerStream = new MediaFoundationReader(fileName);
#else
            throw new InvalidOperationException($"Unsupported file format. Media Foundation reader requires the NAudio.Wasapi package.");
#endif
        }
    }

    /// <summary>
    /// Creates the reader stream from an already-open stream, detecting the format from the
    /// stream contents and ensuring we end up in PCM/IEEE float.
    /// </summary>
    /// <param name="inputStream">The input stream</param>
    private void CreateReaderStream(Stream inputStream)
    {
        if (!inputStream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(inputStream));
        if (!inputStream.CanSeek)
            throw new ArgumentException("Stream must be seekable.", nameof(inputStream));

        switch (DetectStreamFormat(inputStream))
        {
            case StreamAudioFormat.Wave:
                readerStream = new WaveFileReader(inputStream);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
#if !WINDOWS
                    throw new InvalidOperationException("WAV files with non-PCM encoding require Windows for ACM codec conversion");
#else
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
#endif
                }
                break;
            case StreamAudioFormat.Aiff:
                readerStream = new AiffFileReader(inputStream);
                break;
            default:
#if WASAPI
                // fall back to Media Foundation, which detects the format itself and handles
                // MP3, WMA, AAC/MP4, FLAC and more. Media Foundation is the default MP3 path in
                // NAudio 3, so no Mp3FileReader fallback is needed here.
                readerStream = new StreamMediaFoundationReader(inputStream);
                break;
#else
                throw new InvalidOperationException("Unsupported stream format. Only WAV and AIFF streams are supported without the NAudio.Wasapi package (which provides Media Foundation).");
#endif
        }
    }

    private enum StreamAudioFormat
    {
        Wave,
        Aiff,
        Other
    }

    /// <summary>
    /// Peeks at the first few bytes of a stream to identify WAV and AIFF containers by their
    /// magic bytes, restoring the stream position afterwards. Anything else is reported as
    /// <see cref="StreamAudioFormat.Other"/> so it can be handed to Media Foundation.
    /// </summary>
    private static StreamAudioFormat DetectStreamFormat(Stream stream)
    {
        long position = stream.Position;
        Span<byte> header = stackalloc byte[12];
        int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        stream.Position = position;

        if (read >= 12)
        {
            ReadOnlySpan<byte> h = header;
            if (h.Slice(0, 4).SequenceEqual("RIFF"u8) && h.Slice(8, 4).SequenceEqual("WAVE"u8))
                return StreamAudioFormat.Wave;
            if (h.Slice(0, 4).SequenceEqual("FORM"u8) &&
                (h.Slice(8, 4).SequenceEqual("AIFF"u8) || h.Slice(8, 4).SequenceEqual("AIFC"u8)))
                return StreamAudioFormat.Aiff;
        }
        return StreamAudioFormat.Other;
    }

    /// <summary>
    /// File Name (null when this reader was created from a stream)
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// WaveFormat of this stream
    /// </summary>
    public override WaveFormat WaveFormat => sampleChannel.WaveFormat;

    /// <summary>
    /// Length of this stream (in bytes)
    /// </summary>
    public override long Length => length;

    /// <summary>
    /// Position of this stream (in bytes)
    /// </summary>
    public override long Position
    {
        get { return SourceToDest(readerStream.Position); }
        set { lock (lockObject) { readerStream.Position = DestToSource(value); } }
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        var floatSpan = MemoryMarshal.Cast<byte, float>(buffer);
        int samplesRead = Read(floatSpan);
        return samplesRead * 4;
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    /// <summary>
    /// Reads audio samples from this file reader
    /// </summary>
    public int Read(Span<float> buffer)
    {
        lock (lockObject)
        {
            return sampleChannel.Read(buffer);
        }
    }

    /// <summary>
    /// Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
    /// </summary>
    public float Volume
    {
        get { return sampleChannel.Volume; }
        set { sampleChannel.Volume = value; }
    }

    /// <summary>
    /// Helper to convert source to dest bytes
    /// </summary>
    private long SourceToDest(long sourceBytes)
    {
        return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
    }

    /// <summary>
    /// Helper to convert dest to source bytes
    /// </summary>
    private long DestToSource(long destBytes)
    {
        return sourceBytesPerSample * (destBytes / destBytesPerSample);
    }

    /// <summary>
    /// Disposes this AudioFileReader
    /// </summary>
    /// <param name="disposing">True if called from Dispose</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            readerStream?.Dispose();
            readerStream = null;
        }
        base.Dispose(disposing);
    }
}
