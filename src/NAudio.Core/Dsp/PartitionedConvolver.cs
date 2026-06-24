using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Uniformly-partitioned overlap-save FFT convolution for a single channel. Convolves
    /// a real-time sample stream with a fixed impulse response of arbitrary length at a
    /// bounded, constant per-sample cost. The processing latency equals one partition
    /// (see <see cref="LatencySamples"/>); report it for delay compensation.
    /// </summary>
    /// <remarks>
    /// Built on <see cref="FftProcessor"/> (real-input FFT). The impulse-response partition
    /// spectra are pre-scaled by the FFT size so the steady-state path is just a
    /// complex multiply-accumulate per partition plus one inverse FFT per block.
    /// </remarks>
    public sealed class PartitionedConvolver
    {
        private readonly int partition;
        private readonly int fftSize;
        private readonly int spectrumLength;
        private readonly int partitionCount;
        private readonly FftProcessor fft;
        private readonly Complex[][] irSpectra;   // [partition][bin], pre-scaled by fftSize
        private readonly Complex[][] inputFdl;    // ring of recent input-block spectra
        private readonly Complex[] accumulator;
        private readonly float[] window;          // [previous P | new P]
        private readonly float[] timeBlock;       // inverse-FFT scratch (length fftSize)
        private readonly float[] previousTail;    // previous block's P input samples
        private readonly float[] fillBuffer;      // P samples being collected
        private readonly float[] outputBlock;     // P samples ready to emit
        private int fdlIndex;
        private int position;

        /// <summary>
        /// Creates a convolver for the given impulse response.
        /// </summary>
        /// <param name="impulseResponse">Impulse response samples (length ≥ 1).</param>
        /// <param name="partitionSize">FFT partition size; a power of two (e.g. 128–1024).
        /// Larger trades latency for lower CPU.</param>
        public PartitionedConvolver(ReadOnlySpan<float> impulseResponse, int partitionSize)
        {
            if (partitionSize < 1 || (partitionSize & (partitionSize - 1)) != 0)
                throw new ArgumentException("Partition size must be a power of two.", nameof(partitionSize));
            if (impulseResponse.Length < 1)
                throw new ArgumentException("Impulse response must have at least one sample.", nameof(impulseResponse));

            partition = partitionSize;
            fftSize = partitionSize * 2;
            fft = new FftProcessor(fftSize);
            spectrumLength = fft.SpectrumLength;
            partitionCount = (impulseResponse.Length + partition - 1) / partition;

            irSpectra = new Complex[partitionCount][];
            inputFdl = new Complex[partitionCount][];
            for (var k = 0; k < partitionCount; k++)
            {
                irSpectra[k] = new Complex[spectrumLength];
                inputFdl[k] = new Complex[spectrumLength];
            }
            accumulator = new Complex[spectrumLength];
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
                var count = Math.Min(partition, impulseResponse.Length - offset);
                impulseResponse.Slice(offset, count).CopyTo(irBlock);
                fft.RealForward(irBlock, irSpectra[k]);
                // Fold the FFT-pair normalisation into the IR so the runtime path needs
                // no extra scaling: linear-conv = fftSize · inverse(input · ir).
                for (var b = 0; b < spectrumLength; b++)
                {
                    irSpectra[k][b].X *= fftSize;
                    irSpectra[k][b].Y *= fftSize;
                }
            }
        }

        /// <summary>Processing latency in samples (one partition).</summary>
        public int LatencySamples => partition;

        /// <summary>
        /// Processes one input sample and returns one output sample (delayed by
        /// <see cref="LatencySamples"/>).
        /// </summary>
        public float Process(float input)
        {
            var output = outputBlock[position];
            fillBuffer[position] = input;
            position++;
            if (position == partition)
            {
                ProcessBlock();
                position = 0;
            }
            return output;
        }

        /// <summary>
        /// Clears all internal state (input history, output, FDL).
        /// </summary>
        public void Reset()
        {
            for (var k = 0; k < partitionCount; k++)
                Array.Clear(inputFdl[k]);
            Array.Clear(window);
            Array.Clear(timeBlock);
            Array.Clear(previousTail);
            Array.Clear(fillBuffer);
            Array.Clear(outputBlock);
            fdlIndex = 0;
            position = 0;
        }

        private void ProcessBlock()
        {
            // Overlap-save frame: previous P samples followed by the new P samples.
            Array.Copy(previousTail, 0, window, 0, partition);
            Array.Copy(fillBuffer, 0, window, partition, partition);

            fdlIndex = (fdlIndex + 1) % partitionCount;
            fft.RealForward(window, inputFdl[fdlIndex]);

            Array.Clear(accumulator);
            for (var k = 0; k < partitionCount; k++)
            {
                var x = inputFdl[(fdlIndex - k + partitionCount) % partitionCount];
                var h = irSpectra[k];
                for (var b = 0; b < spectrumLength; b++)
                {
                    accumulator[b].X += h[b].X * x[b].X - h[b].Y * x[b].Y;
                    accumulator[b].Y += h[b].X * x[b].Y + h[b].Y * x[b].X;
                }
            }

            fft.RealInverse(accumulator, timeBlock);
            // Overlap-save: the last P samples are the alias-free linear-convolution output.
            Array.Copy(timeBlock, partition, outputBlock, 0, partition);
            Array.Copy(fillBuffer, 0, previousTail, 0, partition);
        }
    }
}
