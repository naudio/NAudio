using System;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;

namespace NAudio.Dsp;

/// <summary>
/// Uniformly-partitioned overlap-save FFT convolution for a single channel. Convolves
/// a real-time sample stream with a fixed impulse response of arbitrary length at a
/// bounded, constant per-sample cost. By default the processing latency equals one
/// partition (see <see cref="LatencySamples"/>); report it for delay compensation.
/// In zero-latency mode the first partition of the impulse response is applied
/// directly in the time domain instead, so there is no latency at all.
/// </summary>
/// <remarks>
/// Built on <see cref="FftProcessor"/> (real-input FFT). The impulse-response partition
/// spectra are pre-scaled by the FFT size so the steady-state path is just a
/// complex multiply-accumulate per partition plus one inverse FFT per block. Spectra
/// are stored as separate real and imaginary arrays so that multiply-accumulate is
/// vectorised.
/// </remarks>
public sealed class PartitionedConvolver
{
    private readonly int partition;
    private readonly int spectrumLength;
    private readonly int partitionCount;      // FFT partitions (zero when the IR fits in the direct head)
    private readonly FftProcessor? fft;
    private readonly float[][] irReal;        // [partition][bin], pre-scaled by fftSize
    private readonly float[][] irImag;
    private readonly float[][] fdlReal;       // ring of recent input-block spectra
    private readonly float[][] fdlImag;
    private readonly float[] accReal;
    private readonly float[] accImag;
    private readonly Complex[] spectrum;      // FFT input/output scratch
    private readonly float[] window;          // [previous P | new P]
    private readonly float[] timeBlock;       // inverse-FFT scratch (length fftSize)
    private readonly float[] previousTail;    // previous block's P input samples
    private readonly float[] fillBuffer;      // P samples being collected
    private readonly float[] outputBlock;     // P samples ready to emit
    private int fdlIndex;
    private int position;

    // Zero-latency mode: the first partition of the IR as a direct-form FIR.
    private readonly float[] headTaps;        // IR head reversed, so a dot product with the history applies it
    private readonly float[] headHistory;     // last headTaps.Length inputs, stored twice so a window never wraps
    private int headPosition;

    /// <summary>
    /// Creates a convolver for the given impulse response, with a latency of one
    /// partition.
    /// </summary>
    /// <param name="impulseResponse">Impulse response samples (length ≥ 1).</param>
    /// <param name="partitionSize">FFT partition size; a power of two (e.g. 128–1024).
    /// Larger trades latency for lower CPU.</param>
    public PartitionedConvolver(ReadOnlySpan<float> impulseResponse, int partitionSize)
        : this(impulseResponse, partitionSize, zeroLatency: false)
    {
    }

    /// <summary>
    /// Creates a convolver for the given impulse response.
    /// </summary>
    /// <param name="impulseResponse">Impulse response samples (length ≥ 1).</param>
    /// <param name="partitionSize">FFT partition size; a power of two.</param>
    /// <param name="zeroLatency">
    /// When true, the first <paramref name="partitionSize"/> samples of the impulse
    /// response are applied as a direct time-domain FIR and the rest by FFT, whose
    /// one-partition delay lines up exactly with that offset, so the output has no
    /// latency. The direct part costs <paramref name="partitionSize"/> multiply-adds
    /// per sample, so small partitions (32–128) suit this mode best.
    /// </param>
    public PartitionedConvolver(ReadOnlySpan<float> impulseResponse, int partitionSize, bool zeroLatency)
    {
        if (partitionSize < 1 || (partitionSize & (partitionSize - 1)) != 0)
            throw new ArgumentException("Partition size must be a power of two.", nameof(partitionSize));
        if (impulseResponse.Length < 1)
            throw new ArgumentException("Impulse response must have at least one sample.", nameof(impulseResponse));

        partition = partitionSize;
        ZeroLatency = zeroLatency;

        var headLength = zeroLatency ? Math.Min(partitionSize, impulseResponse.Length) : 0;
        headTaps = impulseResponse[..headLength].ToArray();
        Array.Reverse(headTaps);
        headHistory = new float[2 * headLength];

        var tail = impulseResponse[headLength..];
        partitionCount = (tail.Length + partition - 1) / partition;
        if (partitionCount == 0)
        {
            irReal = irImag = fdlReal = fdlImag = Array.Empty<float[]>();
            accReal = accImag = window = timeBlock = previousTail = fillBuffer = outputBlock = Array.Empty<float>();
            spectrum = Array.Empty<Complex>();
            return;
        }

        var fftSize = partitionSize * 2;
        fft = new FftProcessor(fftSize);
        spectrumLength = fft.SpectrumLength;
        irReal = new float[partitionCount][];
        irImag = new float[partitionCount][];
        fdlReal = new float[partitionCount][];
        fdlImag = new float[partitionCount][];
        for (var k = 0; k < partitionCount; k++)
        {
            irReal[k] = new float[spectrumLength];
            irImag[k] = new float[spectrumLength];
            fdlReal[k] = new float[spectrumLength];
            fdlImag[k] = new float[spectrumLength];
        }
        accReal = new float[spectrumLength];
        accImag = new float[spectrumLength];
        spectrum = new Complex[spectrumLength];
        window = new float[fftSize];
        timeBlock = new float[fftSize];
        previousTail = new float[partition];
        fillBuffer = new float[partition];
        outputBlock = new float[partition];

        var irBlock = new float[fftSize];
        for (var k = 0; k < partitionCount; k++)
        {
            Array.Clear(irBlock);
            var offset = k * partition;
            var count = Math.Min(partition, tail.Length - offset);
            tail.Slice(offset, count).CopyTo(irBlock);
            fft.RealForward(irBlock, spectrum);
            // Fold the FFT-pair normalisation into the IR so the runtime path needs
            // no extra scaling: linear-conv = fftSize · inverse(input · ir).
            for (var b = 0; b < spectrumLength; b++)
            {
                irReal[k][b] = spectrum[b].X * fftSize;
                irImag[k][b] = spectrum[b].Y * fftSize;
            }
        }
    }

    /// <summary>
    /// Processing latency in samples: one partition, or zero in zero-latency mode.
    /// </summary>
    public int LatencySamples => ZeroLatency ? 0 : partition;

    /// <summary>True if this convolver was created in zero-latency mode.</summary>
    public bool ZeroLatency { get; }

    /// <summary>
    /// Processes one input sample and returns one output sample (delayed by
    /// <see cref="LatencySamples"/>).
    /// </summary>
    public float Process(float input)
    {
        var output = 0f;
        if (partitionCount > 0)
        {
            output = outputBlock[position];
            fillBuffer[position] = input;
            position++;
            if (position == partition)
            {
                ProcessBlock();
                position = 0;
            }
        }

        var headLength = headTaps.Length;
        if (headLength > 0)
        {
            headHistory[headPosition] = input;
            headHistory[headPosition + headLength] = input;
            headPosition++;
            if (headPosition == headLength)
                headPosition = 0;
            // The window starting at headPosition runs oldest to newest input.
            output += TensorPrimitives.Dot(headHistory.AsSpan(headPosition, headLength), headTaps);
        }
        return output;
    }

    /// <summary>
    /// Clears all internal state (input history, output, FDL).
    /// </summary>
    public void Reset()
    {
        for (var k = 0; k < partitionCount; k++)
        {
            Array.Clear(fdlReal[k]);
            Array.Clear(fdlImag[k]);
        }
        Array.Clear(window);
        Array.Clear(timeBlock);
        Array.Clear(previousTail);
        Array.Clear(fillBuffer);
        Array.Clear(outputBlock);
        Array.Clear(headHistory);
        fdlIndex = 0;
        position = 0;
        headPosition = 0;
    }

    private void ProcessBlock()
    {
        // Overlap-save frame: previous P samples followed by the new P samples.
        Array.Copy(previousTail, 0, window, 0, partition);
        Array.Copy(fillBuffer, 0, window, partition, partition);

        fdlIndex = (fdlIndex + 1) % partitionCount;
        fft!.RealForward(window, spectrum);
        var newReal = fdlReal[fdlIndex];
        var newImag = fdlImag[fdlIndex];
        for (var b = 0; b < spectrumLength; b++)
        {
            newReal[b] = spectrum[b].X;
            newImag[b] = spectrum[b].Y;
        }

        Array.Clear(accReal);
        Array.Clear(accImag);
        for (var k = 0; k < partitionCount; k++)
        {
            var j = (fdlIndex - k + partitionCount) % partitionCount;
            ComplexMultiplyAccumulate(irReal[k], irImag[k], fdlReal[j], fdlImag[j], accReal, accImag);
        }

        for (var b = 0; b < spectrumLength; b++)
        {
            spectrum[b].X = accReal[b];
            spectrum[b].Y = accImag[b];
        }
        fft.RealInverse(spectrum, timeBlock);
        // Overlap-save: the last P samples are the alias-free linear-convolution output.
        Array.Copy(timeBlock, partition, outputBlock, 0, partition);
        Array.Copy(fillBuffer, 0, previousTail, 0, partition);
    }

    /// <summary>acc += h · x, element-wise over complex numbers held as separate real and imaginary arrays.</summary>
    private static void ComplexMultiplyAccumulate(float[] hReal, float[] hImag, float[] xReal, float[] xImag, float[] accReal, float[] accImag)
    {
        var n = hReal.Length;
        var b = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var hr = MemoryMarshal.Cast<float, Vector<float>>(hReal.AsSpan());
            var hi = MemoryMarshal.Cast<float, Vector<float>>(hImag.AsSpan());
            var xr = MemoryMarshal.Cast<float, Vector<float>>(xReal.AsSpan());
            var xi = MemoryMarshal.Cast<float, Vector<float>>(xImag.AsSpan());
            var ar = MemoryMarshal.Cast<float, Vector<float>>(accReal.AsSpan());
            var ai = MemoryMarshal.Cast<float, Vector<float>>(accImag.AsSpan());
            for (var v = 0; v < hr.Length; v++)
            {
                ar[v] += hr[v] * xr[v] - hi[v] * xi[v];
                ai[v] += hr[v] * xi[v] + hi[v] * xr[v];
            }
            b = hr.Length * Vector<float>.Count;
        }
        for (; b < n; b++)
        {
            accReal[b] += hReal[b] * xReal[b] - hImag[b] * xImag[b];
            accImag[b] += hReal[b] * xImag[b] + hImag[b] * xReal[b];
        }
    }
}
