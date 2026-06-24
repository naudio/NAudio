using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Produces an output stream by mixing together the channels of a single source using an
    /// arbitrary mixing matrix, where the number of output channels is determined by the number of
    /// columns in the matrix.
    /// </summary>
    /// <remarks>See <see cref="ChannelMixMatrix"/> for some pre-defined matrices.</remarks>
    public class ChannelMixerSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[,] matrix;

        private readonly int inputChannels;
        private readonly int outputChannels;

        private float[] sourceBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelMixerSampleProvider"/> class. The
        /// number of output channels from the mixer depends on the structure of the provided matrix.
        /// </summary>
        /// <param name="source">The provider to read from.</param>
        /// <param name="matrix">
        /// Specifies the matrix that converts input samples to output samples. The number of rows in
        /// the matrix must match the number of input channels from <paramref name="source"/>. The
        /// number of columns in the matrix determines the number of output channels.
        /// </param>
        /// <exception cref="ArgumentNullException">Occurs if any argument is null.</exception>
        /// <exception cref="ArgumentException">
        /// Occurs if the matrix has no output columns, or if the number of rows in the matrix does
        /// not match the number of channels in <paramref name="source"/>.
        /// </exception>
        public ChannelMixerSampleProvider(ISampleProvider source, float[,] matrix)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            this.source = source;
            this.matrix = matrix;
            this.inputChannels = matrix.GetLength(0);
            this.outputChannels = matrix.GetLength(1);

            if (this.outputChannels < 1)
            {
                throw new ArgumentException(
                    "The matrix must have at least one output column.",
                    nameof(matrix)
                );
            }

            if (this.inputChannels != source.WaveFormat.Channels)
            {
                throw new ArgumentException(
                    "The number of channels in the source do not match the number of input rows in the matrix.",
                    nameof(matrix)
                );
            }

            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                this.source.WaveFormat.SampleRate,
                this.outputChannels
            );
        }

        /// <summary>
        /// Gets the WaveFormat of the output from the mixer. The encoding is always <see
        /// cref="WaveFormatEncoding.IeeeFloat"/>. The number of channels present in the <see
        /// cref="WaveFormat"/> depends on the channel mixing matrix in use.
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads samples from the mixer into the provided buffer.
        /// </summary>
        /// <param name="buffer">A buffer to which to write output samples.</param>
        /// <returns>The total number of samples that were obtained.</returns>
        public int Read(Span<float> buffer)
        {
            // 1. Figure out how many whole blocks we can produce, then how many source samples that needs.
            int numBlocks = buffer.Length / this.outputChannels;
            int numSourceSamples = numBlocks * this.inputChannels;

            this.sourceBuffer = BufferHelpers.Ensure(this.sourceBuffer, numSourceSamples);

            // 2. Read from the source and figure out how much we actually got.
            numSourceSamples = source.Read(this.sourceBuffer.AsSpan(0, numSourceSamples));
            numBlocks = numSourceSamples / this.inputChannels;

            for (int i = 0; i < numBlocks; i++)
            {
                var sourceBlock = this.sourceBuffer.AsSpan(this.inputChannels * i, this.inputChannels);
                var destBlock = buffer.Slice(this.outputChannels * i, this.outputChannels);

                TransformBlock(sourceBlock, destBlock);
            }

            return numBlocks * this.outputChannels;
        }

        private void TransformBlock(Span<float> sourceBlock, Span<float> destBlock)
        {
            for (int outCh = 0; outCh < this.outputChannels; outCh++)
            {
                float value = 0;

                for (int inCh = 0; inCh < this.inputChannels; inCh++)
                {
                    value += sourceBlock[inCh] * matrix[inCh, outCh];
                }

                destBlock[outCh] = value;
            }
        }
    }
}
