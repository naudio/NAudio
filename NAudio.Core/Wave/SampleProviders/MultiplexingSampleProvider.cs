using System;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Allows any number of inputs to be patched to outputs
    /// Uses could include swapping left and right channels, turning mono into stereo,
    /// feeding different input sources to different soundcard outputs etc
    /// </summary>
    public class MultiplexingSampleProvider : ISampleProvider
    {
        private readonly IList<ISampleProvider> inputs;
        private readonly WaveFormat waveFormat;
        private readonly int outputChannelCount;
        private readonly int inputChannelCount;
        private readonly List<int> mappings;

        /// <summary>
        /// Creates a multiplexing sample provider, allowing re-patching of input channels to different
        /// output channels
        /// </summary>
        /// <param name="inputs">Input sample providers. Must all be of the same sample rate, but can have any number of channels</param>
        /// <param name="numberOfOutputChannels">Desired number of output channels.</param>
        public MultiplexingSampleProvider(IEnumerable<ISampleProvider> inputs, int numberOfOutputChannels)
        {
            this.inputs = new List<ISampleProvider>(inputs);
            outputChannelCount = numberOfOutputChannels;

            if (this.inputs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one input");
            }
            if (numberOfOutputChannels < 1)
            {
                throw new ArgumentException("You must provide at least one output");
            }
            foreach (var input in this.inputs)
            {
                if (waveFormat == null)
                {
                    if (input.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                    {
                        throw new ArgumentException("Only 32 bit float is supported");
                    }
                    waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, numberOfOutputChannels);
                }
                else
                {
                    if (input.WaveFormat.BitsPerSample != waveFormat.BitsPerSample)
                    {
                        throw new ArgumentException("All inputs must have the same bit depth");
                    }
                    if (input.WaveFormat.SampleRate != waveFormat.SampleRate)
                    {
                        throw new ArgumentException("All inputs must have the same sample rate");
                    }
                }
                inputChannelCount += input.WaveFormat.Channels;
            }

            mappings = new List<int>();
            for (int n = 0; n < outputChannelCount; n++)
            {
                mappings.Add(n % inputChannelCount);
            }
        }

        /// <summary>
        /// persistent temporary buffer to prevent creating work for garbage collector
        /// </summary>
        private float[] inputBuffer;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Buffer to be filled with sample data</param>
        /// <param name="offset">Offset into buffer to start writing to, usually 0</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int sampleFramesRequested = count / outputChannelCount;
            int inputOffset = 0;
            int sampleFramesRead = 0;
            // now we must read from all inputs, even if we don't need their data, so they stay in sync
            foreach (var input in inputs)
            {
                int samplesRequired = sampleFramesRequested * input.WaveFormat.Channels;
                inputBuffer = BufferHelpers.Ensure(inputBuffer, samplesRequired);
                int samplesRead = input.Read(inputBuffer, 0, samplesRequired);
                sampleFramesRead = Math.Max(sampleFramesRead, samplesRead / input.WaveFormat.Channels);

                for (int n = 0; n < input.WaveFormat.Channels; n++)
                {
                    int inputIndex = inputOffset + n;
                    for (int outputIndex = 0; outputIndex < outputChannelCount; outputIndex++)
                    {
                        if (mappings[outputIndex] == inputIndex)
                        {
                            int inputBufferOffset = n;
                            int outputBufferOffset = offset + outputIndex;
                            int sample = 0;
                            while (sample < sampleFramesRequested && inputBufferOffset < samplesRead)
                            {
                                buffer[outputBufferOffset] = inputBuffer[inputBufferOffset];
                                outputBufferOffset += outputChannelCount;
                                inputBufferOffset += input.WaveFormat.Channels;
                                sample++;
                            }
                            // clear the end
                            while (sample < sampleFramesRequested)
                            {
                                buffer[outputBufferOffset] = 0;
                                outputBufferOffset += outputChannelCount;
                                sample++;
                            }
                        }
                    }
                }
                inputOffset += input.WaveFormat.Channels;
            }

            return sampleFramesRead * outputChannelCount;
        }

        /// <summary>
        /// The output WaveFormat for this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Connects a specified input channel to an output channel
        /// </summary>
        /// <param name="inputChannel">Input Channel index (zero based). Must be less than InputChannelCount</param>
        /// <param name="outputChannel">Output Channel index (zero based). Must be less than OutputChannelCount</param>
        public void ConnectInputToOutput(int inputChannel, int outputChannel)
        {
            if (inputChannel < 0 || inputChannel >= InputChannelCount)
            {
                throw new ArgumentException("Invalid input channel");
            }
            if (outputChannel < 0 || outputChannel >= OutputChannelCount)
            {
                throw new ArgumentException("Invalid output channel");
            }
            mappings[outputChannel] = inputChannel;
        }

        /// <summary>
        /// The number of input channels. Note that this is not the same as the number of input wave providers. If you pass in
        /// one stereo and one mono input provider, the number of input channels is three.
        /// </summary>
        public int InputChannelCount => inputChannelCount;

        /// <summary>
        /// The number of output channels, as specified in the constructor.
        /// </summary>
        public int OutputChannelCount => outputChannelCount;
    }
}
