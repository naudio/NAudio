using System;
using NAudio.Dmo;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Wave Stream for converting between sample rates
/// </summary>
public class ResamplerDmoStream : WaveStream
{
    private readonly IWaveProvider inputProvider;
    private readonly WaveStream inputStream;
    private readonly WaveFormat outputFormat;
    private DmoOutputDataBuffer outputBuffer;
    private DmoOutputDataBuffer[] outputBufferArray;
    private DmoResampler dmoResampler;
    private MediaBuffer inputMediaBuffer;
    private byte[] inputBuffer;
    private byte[] outputStaging;
    private int outputStagingOffset;
    private int outputStagingCount;
    private bool endOfStreamSignaled;
    private long position;

    /// <summary>
    /// WaveStream to resample using the DMO Resampler
    /// </summary>
    /// <param name="inputProvider">Input audio source</param>
    /// <param name="outputFormat">Desired Output Format</param>
    public ResamplerDmoStream(IWaveProvider inputProvider, WaveFormat outputFormat)
    {
        this.inputProvider = inputProvider;
        inputStream = inputProvider as WaveStream;
        this.outputFormat = outputFormat;
        dmoResampler = new DmoResampler();
        if (!dmoResampler.MediaObject.SupportsInputWaveFormat(0, inputProvider.WaveFormat))
        {
            throw new ArgumentException("Unsupported Input Stream format", nameof(inputProvider));
        }

        dmoResampler.MediaObject.SetInputWaveFormat(0, inputProvider.WaveFormat);
        if (!dmoResampler.MediaObject.SupportsOutputWaveFormat(0, outputFormat))
        {
            throw new ArgumentException("Unsupported Output Stream format", nameof(outputFormat));
        }

        dmoResampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
        if (inputStream != null)
        {
            position = InputToOutputPosition(inputStream.Position);
        }
        inputMediaBuffer = new MediaBuffer(inputProvider.WaveFormat.AverageBytesPerSecond);
        outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond);
        outputBufferArray = new[] { outputBuffer };
    }

    /// <summary>
    /// Stream Wave Format
    /// </summary>
    public override WaveFormat WaveFormat => outputFormat;

    private long InputToOutputPosition(long inputPosition)
    {
        double ratio = (double)outputFormat.AverageBytesPerSecond
            / inputProvider.WaveFormat.AverageBytesPerSecond;
        long outputPosition = (long)(inputPosition * ratio);
        if (outputPosition % outputFormat.BlockAlign != 0)
        {
            outputPosition -= outputPosition % outputFormat.BlockAlign;
        }
        return outputPosition;
    }

    private long OutputToInputPosition(long outputPosition)
    {
        double ratio = (double)outputFormat.AverageBytesPerSecond
            / inputProvider.WaveFormat.AverageBytesPerSecond;
        long inputPosition = (long)(outputPosition / ratio);
        if (inputPosition % inputProvider.WaveFormat.BlockAlign != 0)
        {
            inputPosition -= inputPosition % inputProvider.WaveFormat.BlockAlign;
        }
        return inputPosition;
    }

    /// <summary>
    /// Stream length in bytes
    /// </summary>
    public override long Length
    {
        get
        {
            if (inputStream == null)
            {
                throw new InvalidOperationException("Cannot report length if the input was not a WaveStream");
            }
            return InputToOutputPosition(inputStream.Length);
        }
    }

    /// <summary>
    /// Stream position in bytes
    /// </summary>
    public override long Position
    {
        get
        {
            return position;
        }
        set
        {
            if (inputStream == null)
            {
                throw new InvalidOperationException("Cannot set position if the input was not a WaveStream");
            }
            inputStream.Position = OutputToInputPosition(value);
            position = InputToOutputPosition(inputStream.Position);
            // Discontinuity puts the DMO into drain-only mode until its buffered
            // output is flushed; do that here and discard, so the next Read starts
            // cleanly from the new input position rather than emitting old-position tail.
            dmoResampler.MediaObject.Discontinuity(0);
            DrainAndDiscard();
            outputStagingOffset = 0;
            outputStagingCount = 0;
            endOfStreamSignaled = false;
        }
    }

    /// <summary>
    /// Reads data from input stream
    /// </summary>
    /// <param name="buffer">buffer</param>
    /// <param name="offset">offset into buffer</param>
    /// <param name="count">Bytes required</param>
    /// <returns>Number of bytes read</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Reads resampled data into a span (zero-copy output path)
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        int outputBytesProvided = 0;

        while (outputBytesProvided < buffer.Length)
        {
            if (outputStagingCount > 0)
            {
                outputBytesProvided += ConsumeStaging(buffer.Slice(outputBytesProvided));
                continue;
            }

            if (DrainOnceIntoStaging() > 0)
            {
                continue;
            }

            if (endOfStreamSignaled)
            {
                break;
            }

            if (!dmoResampler.MediaObject.IsAcceptingData(0))
            {
                // Defensive break: with the Position setter draining on seek and
                // the EOS path handled below, the DMO should always accept input
                // here. Spinning would replicate the historic infinite-loop bug.
                break;
            }

            int inputBytesRequired = (int)OutputToInputPosition(buffer.Length - outputBytesProvided);
            int blockAlign = inputProvider.WaveFormat.BlockAlign;
            if (inputBytesRequired < blockAlign)
            {
                // For tiny remaining output sizes the block-aligned input request
                // can round to zero; upstream would then return 0 and look like EOS.
                // Ask for at least one block - any output overrun lives in staging.
                inputBytesRequired = blockAlign;
            }
            inputBuffer = BufferHelpers.Ensure(inputBuffer, inputBytesRequired);
            int inputBytesRead = inputProvider.Read(inputBuffer.AsSpan(0, inputBytesRequired));
            if (inputBytesRead == 0)
            {
                // Upstream EOS - tell the DMO so it flushes the tail samples still
                // inside its resampler kernel; the next iteration drains them.
                dmoResampler.MediaObject.Discontinuity(0);
                endOfStreamSignaled = true;
                continue;
            }
            inputMediaBuffer.LoadData(inputBuffer.AsSpan(0, inputBytesRead));
            dmoResampler.MediaObject.ProcessInput(0, inputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);
        }

        position += outputBytesProvided;
        return outputBytesProvided;
    }

    private int DrainOnceIntoStaging()
    {
        outputBuffer.MediaBuffer.SetLength(0);
        outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;
        // DmoOutputDataBuffer is a struct; copy current state into the cached
        // single-element array so ProcessOutput sees the freshly reset flags.
        outputBufferArray[0] = outputBuffer;
        dmoResampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, outputBufferArray);

        int produced = outputBuffer.Length;
        if (produced == 0)
        {
            return 0;
        }

        outputStaging = BufferHelpers.Ensure(outputStaging, produced);
        outputBuffer.RetrieveData(outputStaging.AsSpan(0, produced));
        outputStagingOffset = 0;
        outputStagingCount = produced;
        return produced;
    }

    private int ConsumeStaging(Span<byte> destination)
    {
        int n = Math.Min(outputStagingCount, destination.Length);
        if (n == 0)
        {
            return 0;
        }
        outputStaging.AsSpan(outputStagingOffset, n).CopyTo(destination);
        outputStagingOffset += n;
        outputStagingCount -= n;
        if (outputStagingCount == 0)
        {
            outputStagingOffset = 0;
        }
        return n;
    }

    private void DrainAndDiscard()
    {
        while (DrainOnceIntoStaging() > 0)
        {
            outputStagingOffset = 0;
            outputStagingCount = 0;
        }
    }

    /// <summary>
    /// Dispose
    /// </summary>
    /// <param name="disposing">True if disposing (not from finalizer)</param>
    protected override void Dispose(bool disposing)
    {
        if (inputMediaBuffer != null)
        {
            inputMediaBuffer.Dispose();
            inputMediaBuffer = null;
        }
        outputBuffer.Dispose();
        if (dmoResampler != null)
        {
            //resampler.Dispose(); s
            dmoResampler = null;
        }
        base.Dispose(disposing);
    }
}
