using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace NAudioWpfDemo.MediaFoundationEncode
{
    /// <summary>
    /// Prototyping a Media Foundation Encoder to be part of NAudio
    /// </summary>
    class MediaFoundationEncoder : IDisposable
    {
        /// <summary>
        /// Queries the available bitrates for a given encoding output type, sample rate and number of channels
        /// </summary>
        /// <param name="audioSubtype"></param>
        /// <param name="sampleRate">The sample rate of the PCM to encode</param>
        /// <param name="channels">The number of channels of the PCM to encode</param>
        /// <returns>An array of available bitrates in average bits per second</returns>
        public static int[] GetEncodeBitrates(Guid audioSubtype, int sampleRate, int channels)
        {
            var bitRates = new HashSet<int>();
            IMFCollection availableTypes;
            MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(
                audioSubtype, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, null, out availableTypes);
            int count;
            availableTypes.GetElementCount(out count);
            for (int n = 0; n < count; n++)
            {
                object mediaTypeObject;
                availableTypes.GetElement(n, out mediaTypeObject);
                var mediaType = (IMFMediaType)mediaTypeObject;
                int bytesPerSecond;
                mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, out bytesPerSecond);
                bitRates.Add(bytesPerSecond*8);
                Marshal.ReleaseComObject(mediaType);
            }
            Marshal.ReleaseComObject(availableTypes);
            return bitRates.ToArray();
        }

        public static void EncodeToWma(IWaveProvider inputProvider, string outputFile, int desiredBitRate)
        {
            using (var encoder = new MediaFoundationEncoder(AudioSubtypes.MFAudioFormat_WMAudioV8, inputProvider.WaveFormat,
                                                         desiredBitRate))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        public static void EncodeToMp3(IWaveProvider inputProvider, string outputFile, int desiredBitRate)
        {
            using (var encoder = new MediaFoundationEncoder(AudioSubtypes.MFAudioFormat_MP3, inputProvider.WaveFormat,
                                                         desiredBitRate))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        public static void EncodeToAac(IWaveProvider inputProvider, string outputFile, int desiredBitRate)
        {
            using (var encoder = new MediaFoundationEncoder(AudioSubtypes.MFAudioFormat_AAC, inputProvider.WaveFormat,
                                             desiredBitRate))
            {
                // should AAC container have ADTS, or is that just for ADTS?
                // http://www.hydrogenaudio.org/forums/index.php?showtopic=97442
                encoder.Encode(outputFile, inputProvider);
            }
        }

        private static IMFMediaType SelectMediaType(Guid outputFormat, WaveFormat inputFormat, int desiredBitRate)
        {
            IMFCollection availableTypes;
            int avgBitrateDiff = int.MaxValue;
            int desiredBytesPerSecond = desiredBitRate/8;
            IMFMediaType selectedType = null;

            MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(
                outputFormat, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, null, out availableTypes);
            int count;
            availableTypes.GetElementCount(out count);
            for (int n = 0; n < count; n++)
            {
                object mediaTypeObject;
                availableTypes.GetElement(n, out mediaTypeObject);
                var mediaType = (IMFMediaType)mediaTypeObject;

                // filter out types that are for the wrong sample rate and channels
                int samplesPerSecond;
                mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, out samplesPerSecond);
                if (inputFormat.SampleRate != samplesPerSecond)
                    continue;
                int channelCount;
                mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, out channelCount);
                if (inputFormat.Channels != channelCount)
                    continue;

                // Get the byte per second
                int avgBytePerSecond;
                mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, out avgBytePerSecond);

                // If this is better than the last one found remember the index
                var diff = Math.Abs(avgBytePerSecond - desiredBytesPerSecond);
                if (diff < avgBitrateDiff)
                {
                    if (selectedType != null)
                    {
                        Marshal.ReleaseComObject(selectedType);
                    }
                    selectedType = mediaType;
                    avgBitrateDiff = diff;
                }
                else
                {
                    Marshal.ReleaseComObject(mediaType);
                }
            }
            Marshal.ReleaseComObject(availableTypes);
            return selectedType;
        }

        private readonly Guid outputAudioSubtype;
        private readonly WaveFormat inputWaveFormat;
        private readonly int selectedBitRate;
        private readonly IMFMediaType outputMediaType;
        private readonly IMFMediaType inputMediaType;
        private bool disposed;

        public MediaFoundationEncoder(Guid outputAudioSubtype, WaveFormat inputFormat, int desiredBitRate)
        {
            if (inputFormat.Encoding != WaveFormatEncoding.Pcm && inputFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }
            this.outputAudioSubtype = outputAudioSubtype;
            this.inputWaveFormat = inputFormat;
            outputMediaType = SelectMediaType(outputAudioSubtype, inputFormat, desiredBitRate);
            inputMediaType = MediaFoundationApi.CreateMediaTypeFromWaveFormat(inputWaveFormat);
        }

        public void Encode(string outputFile, IWaveProvider inputProvider)
        {
            ValidateInputFormat(inputProvider);

            // TODO: should validate the extension?

            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            IMFSinkWriter writer;
            MediaFoundationInterop.MFCreateSinkWriterFromURL(outputFile, null, null, out writer);
            try
            {
                int streamIndex;
                writer.AddStream(outputMediaType, out streamIndex);

                // n.b. can get 0xC00D36B4 - MF_E_INVALIDMEDIATYPE here
                writer.SetInputMediaType(streamIndex, inputMediaType, null);

                PerformEncode(writer, streamIndex, inputProvider);
            }
            finally
            {
                Marshal.ReleaseComObject(writer);
            }
        }

        private void ValidateInputFormat(IWaveProvider inputProvider)
        {
            if (inputProvider.WaveFormat.Encoding != inputWaveFormat.Encoding)
            {
                throw new ArgumentException("Cannot change the encoding selected in the constructor");
            }
            if (inputProvider.WaveFormat.SampleRate != inputWaveFormat.SampleRate)
            {
                throw new ArgumentException(
                    "Cannot change the encoding selected in the constructor - sample rate must be the same");
            }
            if (inputProvider.WaveFormat.BitsPerSample != inputWaveFormat.BitsPerSample)
            {
                throw new ArgumentException(
                    "Cannot change the encoding selected in the constructor - bit depth must be the same");
            }
            if (inputProvider.WaveFormat.Channels != inputWaveFormat.Channels)
            {
                throw new ArgumentException(
                    "Cannot change the encoding selected in the constructor - channel count must be the same");
            }
        }

        private void PerformEncode(IMFSinkWriter writer, int streamIndex, IWaveProvider inputProvider)
        {
            int maxLength = inputWaveFormat.AverageBytesPerSecond * 4;
            var managedBuffer = new byte[maxLength];

            writer.BeginWriting();

            long position = 0;
            long duration = 0;
            do
            {
                duration = ConvertOneBuffer(writer, streamIndex, inputProvider, position, managedBuffer);
                position += duration;
            } while (duration > 0);

            writer.DoFinalize();
        }

        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        private long ConvertOneBuffer(IMFSinkWriter writer, int streamIndex, IWaveProvider inputProvider, long position, byte[] managedBuffer)
        {
            long durationConverted = 0;
            int maxLength;
            IMFMediaBuffer buffer =
    MediaFoundationApi.CreateMemoryBuffer(managedBuffer.Length);
            buffer.GetMaxLength(out maxLength);

            IMFSample sample = MediaFoundationApi.CreateSample();
            sample.AddBuffer(buffer);

            IntPtr ptr;
            int currentLength;
            buffer.Lock(out ptr, out maxLength, out currentLength);
            int read = inputProvider.Read(managedBuffer, 0, maxLength);
            if (read > 0)
            {
                durationConverted = BytesToNsPosition(read, inputWaveFormat);
                Marshal.Copy(managedBuffer, 0, ptr, read);
                buffer.SetCurrentLength(read);
                buffer.Unlock();
                sample.SetSampleTime(position);
                sample.SetSampleDuration(durationConverted);
                writer.WriteSample(streamIndex, sample);
                //writer.Flush(streamIndex);
            }
            else
            {
                buffer.Unlock();
            }

            Marshal.ReleaseComObject(sample);
            Marshal.ReleaseComObject(buffer);
            return durationConverted;
        }

        protected void Dispose(bool disposing)
        {
            Marshal.ReleaseComObject(inputMediaType);
            Marshal.ReleaseComObject(outputMediaType);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        ~MediaFoundationEncoder()
        {
            Dispose(false);
        }
    }
}
