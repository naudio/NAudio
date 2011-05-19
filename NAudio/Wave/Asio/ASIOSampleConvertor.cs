using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// This class stores convertors for different interleaved WaveFormat to ASIOSampleType separate channel
    /// format.
    /// </summary>
    internal class ASIOSampleConvertor
    {
        public delegate void SampleConvertor(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples);

        /// <summary>
        /// Selects the sample convertor based on the input WaveFormat and the output ASIOSampleTtype.
        /// </summary>
        /// <param name="waveFormat">The wave format.</param>
        /// <param name="asioType">The type.</param>
        /// <returns></returns>
        public static SampleConvertor SelectSampleConvertor(WaveFormat waveFormat, ASIOSampleType asioType)
        {
            SampleConvertor convertor = null;
            bool is2Channels = waveFormat.Channels == 2;

            // TODO : IMPLEMENTS OTHER CONVERTOR TYPES
            switch (asioType)
            {
                case ASIOSampleType.ASIOSTInt32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToInt2Channels : (SampleConvertor)ConvertorShortToIntGeneric;
                            break;
                        case 32:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToInt2Channels : (SampleConvertor)ConvertorFloatToIntGeneric;
                            break;
                    }
                    break;
                case ASIOSampleType.ASIOSTInt16LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToShort2Channels : (SampleConvertor)ConvertorShortToShortGeneric;
                            break;
                        case 32:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToShort2Channels : (SampleConvertor)ConvertorFloatToShortGeneric;
                            break;
                    }
                    break;
                case ASIOSampleType.ASIOSTInt24LSB:                    
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            convertor = ConverterFloatTo24LSBGeneric;
                            break;
                    }
                    break;
                case ASIOSampleType.ASIOSTFloat32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            convertor = ConverterFloatToFloatGeneric;
                            break;
                    }
                    break;

                default:
                    throw new ArgumentException(
                        String.Format("ASIO Buffer Type {0} is not yet supported.",
                                      Enum.GetName(typeof(ASIOSampleType), asioType)));
            }
            return convertor;
        }


        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        public static void ConvertorShortToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                leftSamples++;
                rightSamples++;
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples = inputSamples[0];
                    *rightSamples = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                    // Add 4 Bytes
                    leftSamples += 2;
                    rightSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        public static void ConvertorShortToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                    // Point to upper 16 bits of the 32Bits.
                    samples[i]++;
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j] = *inputSamples++;
                        samples[j] += 2;
                    }
                }
            }
        }


        /// <summary>
        /// Optimized convertor for 2 channels FLOAT
        /// </summary>
        public static void ConvertorFloatToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                int* leftSamples = (int*)asioOutputBuffers[0];
                int* rightSamples = (int*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToInt(inputSamples[0]);
                    *rightSamples++ = clampToInt(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor SHORT
        /// </summary>
        public static void ConvertorFloatToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                float*[] samples = new float*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = clampToInt(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        public static void ConvertorShortToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        public static void ConvertorShortToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }


        /// <summary>
        /// Optimized convertor for 2 channels FLOAT
        /// </summary>
        public static void ConvertorFloatToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToShort(inputSamples[0]);
                    *rightSamples++ = clampToShort(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor SHORT
        /// </summary>
        public static void ConvertorFloatToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = clampToShort(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Generic converter 24 LSB
        /// </summary>
        public static void ConverterFloatTo24LSBGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                
                byte*[] samples = new byte*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (byte*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        int sample24 = clampTo24Bit(*inputSamples++);
                        *(samples[j]++) = (byte)(sample24);
                        *(samples[j]++) = (byte)(sample24 >> 8);
                        *(samples[j]++) = (byte)(sample24 >> 16);
                    }
                }
            }
        }

        /// <summary>
        /// Generic convertor for float
        /// </summary>
        public static void ConverterFloatToFloatGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                float*[] samples = new float*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }

        private static int clampTo24Bit(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (int)(sampleValue * 8388607.0);
        }

        private static int clampToInt(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (int)(sampleValue * 2147483647.0);
        }

        private static short clampToShort(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (short)(sampleValue * 32767.0);
        }
    }
}
