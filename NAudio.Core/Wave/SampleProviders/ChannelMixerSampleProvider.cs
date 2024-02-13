using System;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.Core.Wave.SampleProviders
{
    /// <summary>
    /// Produces an output stream by mixing together the channels of a single source using an
    /// arbitrary mixing matrix, where the number of output channels is determined by the number of
    /// columns in the matrix.
    /// </summary>
    /// <remarks>See <see cref="ChannelMixMatrix"/> for some pre-defined matrixes.</remarks>
    public class ChannelMixerSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[,] matrix;

        private readonly int inputChannels;
        private readonly int outputChannels;

        private float[] sourceBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelMixer"/> class. The number of output
        /// channels from the mixer depends on the structure of the provided matrix.
        /// </summary>
        /// <param name="source">The provider to read from.</param>
        /// <param name="matrix">
        /// Specifies the matrix that converts input samples to output samples. The values of the
        /// matrix should be between 0.0f and 1.0f. The number of rows in the matrix must match the
        /// number of input channels from <paramref name="source"/>. The number of columns in the matrix
        /// determines the number of output channels.
        /// </param>
        /// <exception cref="ArgumentNullException">Occurs if any argument is null.</exception>
        /// <exception cref="ArgumentException">Occurs if the matrix is not 2-dimensional.</exception>
        public ChannelMixerSampleProvider( ISampleProvider source, float[,] matrix )
        {
            if( source == null )
                throw new ArgumentNullException( nameof( source ) );

            if( matrix == null )
                throw new ArgumentNullException( nameof( matrix ) );

            this.source = source;
            this.matrix = matrix;
            this.inputChannels = matrix.GetLength( 0 );
            this.outputChannels = matrix.GetLength( 1 );

            if( this.inputChannels != source.WaveFormat.Channels )
            {
                throw new ArgumentException(
                    "The number of channels in the source do not match the number of input elements in the matrix."
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
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Reads samples from the mixer into the provided buffer.
        /// </summary>
        /// <param name="buffer">A buffer to which to write output samples.</param>
        /// <param name="start">The first index in 'buffer' to write to.</param>
        /// <param name="numSamples">
        /// The maximum number of samples to write to the buffer. Note that fewer than this number
        /// of samples may be returned.
        /// </param>
        /// <returns>The total number of samples that were obtained.</returns>
        public int Read( float[] buffer, int start, int numSamples )
        {
            // 1. Figure out how many samples to read from the source to satisfy the caller.
            int numBlocks = numSamples / this.outputChannels;
            int numSourceSamples = numBlocks * this.inputChannels;

            this.sourceBuffer = BufferHelpers.Ensure( this.sourceBuffer, numSourceSamples );

            // 2. Read from the source and figure out how much we got.
            numSourceSamples = source.Read( this.sourceBuffer, 0, numSourceSamples );
            numBlocks = numSourceSamples / this.inputChannels;

            // 3. Build a view over the input and output float arrays that will view just one block.
            var sourceBlock = new FloatSpan( this.sourceBuffer );
            var destBlock = new FloatSpan( buffer );

            for( int i = 0; i < numBlocks; i++ )
            {
                // 4. Update which block we're looking at.
                sourceBlock.Start = this.inputChannels * i;
                destBlock.Start = this.outputChannels * i + start;

                // 5. Transform one block from input to output.
                TransformBlock( sourceBlock, destBlock );
            }

            return numBlocks * this.outputChannels;
        }

        private void TransformBlock( FloatSpan sourceBlock, FloatSpan destBlock )
        {
            float value;
            for( int outCh = 0; outCh < this.outputChannels; outCh++ )
            {
                value = 0;

                for( int inCh = 0; inCh < this.inputChannels; inCh++ )
                {
                    value += sourceBlock[inCh] * matrix[inCh, outCh];
                }

                destBlock[outCh] = value;
            }
        }

        /// <summary>
        /// Makes it easier to do the index math when transforming blocks.
        /// </summary>
        private struct FloatSpan
        {
            public float[] Floats;

            public int Start;

            public FloatSpan( float[] floats )
            {
                this.Floats = floats;
                this.Start = 0;
            }

            public float this[int index]
            {
                get => Floats[index + Start];
                set => Floats[index + Start] = value;
            }
        }
    }
}
