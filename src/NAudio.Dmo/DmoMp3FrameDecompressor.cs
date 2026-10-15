using System;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dmo;

/// <summary>
/// MP3 Frame decompressor using the Windows Media MP3 Decoder DMO object
/// </summary>
public class DmoMp3FrameDecompressor : IMp3FrameDecompressor
{
    private WindowsMediaMp3Decoder? mp3Decoder;
    private readonly WaveFormat pcmFormat;
    private MediaBuffer? inputMediaBuffer;
    private DmoOutputDataBuffer outputBuffer;
    private readonly DmoOutputDataBuffer[] outputBufferArray;
    private bool reposition;

    private WindowsMediaMp3Decoder Decoder => mp3Decoder ?? throw new ObjectDisposedException(nameof(DmoMp3FrameDecompressor));
    private MediaBuffer InputMediaBuffer => inputMediaBuffer ?? throw new ObjectDisposedException(nameof(DmoMp3FrameDecompressor));

    /// <summary>
    /// Initializes a new instance of the DMO MP3 Frame decompressor
    /// </summary>
    /// <param name="sourceFormat"></param>
    public DmoMp3FrameDecompressor(WaveFormat sourceFormat)
    {
        this.mp3Decoder = new WindowsMediaMp3Decoder();
        if (!mp3Decoder.MediaObject.SupportsInputWaveFormat(0, sourceFormat))
        {
            throw new ArgumentException("Unsupported input format");
        }
        mp3Decoder.MediaObject.SetInputWaveFormat(0, sourceFormat);
        pcmFormat = new WaveFormat(sourceFormat.SampleRate, sourceFormat.Channels); // 16 bit
        if (!mp3Decoder.MediaObject.SupportsOutputWaveFormat(0, pcmFormat))
        {
            throw new ArgumentException($"Unsupported output format {pcmFormat}");
        }
        mp3Decoder.MediaObject.SetOutputWaveFormat(0, pcmFormat);

        // a second is more than enough to decompress a frame at a time
        inputMediaBuffer = new MediaBuffer(sourceFormat.AverageBytesPerSecond);
        outputBuffer = new DmoOutputDataBuffer(pcmFormat.AverageBytesPerSecond);
        outputBufferArray = new[] { outputBuffer };
    }

    /// <summary>
    /// Converted PCM WaveFormat
    /// </summary>
    public WaveFormat OutputFormat { get { return pcmFormat; } }

    /// <summary>
    /// Decompress a single frame of MP3 into the supplied span.
    /// </summary>
    public int DecompressFrame(Mp3Frame frame, Span<byte> dest)
    {
        if (frame.RawData == null)
        {
            throw new ArgumentException("The Mp3Frame was read without its data", nameof(frame));
        }
        // 1. copy into our DMO's input buffer
        InputMediaBuffer.LoadData(frame.RawData.AsSpan(0, frame.FrameLength));

        if (reposition)
        {
            Decoder.MediaObject.Flush();
            reposition = false;
        }

        // 2. Give the input buffer to the DMO to process
        Decoder.MediaObject.ProcessInput(0, InputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);

        outputBuffer.MediaBuffer.SetLength(0);
        outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;

        // 3. Now ask the DMO for some output data.
        // DmoOutputDataBuffer is a struct; refresh the cached single-element
        // array slot so ProcessOutput sees the freshly reset flags.
        outputBufferArray[0] = outputBuffer;
        Decoder.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, outputBufferArray);

        if (outputBuffer.Length == 0)
        {
            Debug.WriteLine("ResamplerDmoStream.Read: No output data available");
            return 0;
        }

        // 5. Now get the data out of the output buffer
        outputBuffer.RetrieveData(dest);
        Debug.Assert(!outputBuffer.MoreDataAvailable, "have not implemented more data available yet");

        return outputBuffer.Length;
    }

    /// <summary>
    /// Decompress a single frame of MP3.
    /// </summary>
    public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        => DecompressFrame(frame, dest.AsSpan(destOffset));

    /// <summary>
    /// Alerts us that a reposition has occurred so the MP3 decoder needs to reset its state
    /// </summary>
    public void Reset()
    {
        reposition = true;
    }

    /// <summary>
    /// Dispose of this object and clean up resources
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        inputMediaBuffer?.Dispose();
        inputMediaBuffer = null;
        outputBuffer.Dispose();
        mp3Decoder?.Dispose();
        mp3Decoder = null;
    }
}
