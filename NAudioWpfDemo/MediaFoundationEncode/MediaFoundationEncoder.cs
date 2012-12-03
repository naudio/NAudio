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
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.SampleRate == sampleRate && mt.ChannelCount == channels)
                .Select(mt => mt.AverageBytesPerSecond*8)
                .Distinct()
                .OrderBy(br => br)
                .ToArray();
        }

        public static MediaType[] GetOutputMediaTypes(Guid audioSubtype)
        {
            IMFCollection availableTypes;
            try
            {
                MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(
                    audioSubtype, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, null, out availableTypes);

            }
            catch (COMException c)
            {
                if (c.ErrorCode == MediaFoundationErrors.MF_E_NOT_FOUND)
                {
                    // Don't worry if we didn't find any - just means no encoder available for this type
                    return new MediaType[0];
                }
                else
                {
                    throw;
                }
            }
            int count;
            availableTypes.GetElementCount(out count);
            var mediaTypes = new List<MediaType>(count);
            for (int n = 0; n < count; n++)
            {
                object mediaTypeObject;
                availableTypes.GetElement(n, out mediaTypeObject);
                var mediaType = (IMFMediaType)mediaTypeObject;
                mediaTypes.Add(new MediaType(mediaType));
            }
            Marshal.ReleaseComObject(availableTypes);
            return mediaTypes.ToArray();
        }

        public static void EncodeToWma(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputProvider.WaveFormat, desiredBitRate);
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        public static void EncodeToMp3(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputProvider.WaveFormat, desiredBitRate);
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        public static void EncodeToAac(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            // Information on configuring an AAC media type can be found here:
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputProvider.WaveFormat, desiredBitRate);
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                // should AAC container have ADTS, or is that just for ADTS?
                // http://www.hydrogenaudio.org/forums/index.php?showtopic=97442
                encoder.Encode(outputFile, inputProvider);
            }
        }

        public static MediaType SelectMediaType(Guid audioSubtype, WaveFormat inputFormat, int desiredBitRate)
        {
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.SampleRate == inputFormat.SampleRate && mt.ChannelCount == inputFormat.Channels)
                .Select(mt => new { MediaType = mt, Delta = Math.Abs(desiredBitRate - mt.AverageBytesPerSecond * 8) } )
                .OrderBy(mt => mt.Delta)
                .Select(mt => mt.MediaType)
                .FirstOrDefault();
        }

        private readonly MediaType outputMediaType;
        private bool disposed;

        public MediaFoundationEncoder(MediaType outputMediaType)
        {
            this.outputMediaType = outputMediaType;
        }

        public void Encode(string outputFile, IWaveProvider inputProvider)
        {
            if (inputProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            var inputMediaType = new MediaType(inputProvider.WaveFormat);

            // TODO: should validate the extension?

            var writer = CreateSinkWriter(outputFile);
            try
            {
                int streamIndex;
                writer.AddStream(outputMediaType.MediaFoundationObject, out streamIndex);

                // n.b. can get 0xC00D36B4 - MF_E_INVALIDMEDIATYPE here
                writer.SetInputMediaType(streamIndex, inputMediaType.MediaFoundationObject, null);

                PerformEncode(writer, streamIndex, inputProvider);
            }
            finally
            {
                Marshal.ReleaseComObject(writer);
                Marshal.ReleaseComObject(inputMediaType.MediaFoundationObject);
            }
        }

        private static IMFSinkWriter CreateSinkWriter(string outputFile)
        {
            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension 
            // n.b. AAC encode on Win 8 can have AAC extension, but use MP4 in win 7
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            IMFSinkWriter writer;
            var attributes = MediaFoundationApi.CreateAttributes(1);
            attributes.SetUINT32(MediaFoundationAttributes.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);
            try
            {
                MediaFoundationInterop.MFCreateSinkWriterFromURL(outputFile, null, attributes, out writer);
            }
            catch (COMException e)
            {
                if (e.ErrorCode == MediaFoundationErrors.MF_E_NOT_FOUND)
                {
                    throw new ArgumentException("Was not able to create a sink writer for this file extension");
                }
                throw;
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }
            return writer;
        }

        private void PerformEncode(IMFSinkWriter writer, int streamIndex, IWaveProvider inputProvider)
        {
            int maxLength = inputProvider.WaveFormat.AverageBytesPerSecond * 4;
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
                durationConverted = BytesToNsPosition(read, inputProvider.WaveFormat);
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
            Marshal.ReleaseComObject(outputMediaType.MediaFoundationObject);
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

    class MediaType
    {
        private readonly IMFMediaType mediaType;

        public MediaType(IMFMediaType mediaType)
        {
            this.mediaType = mediaType;
        }

        public MediaType()
        {
            this.mediaType = MediaFoundationApi.CreateMediaType();
        }

        public MediaType(WaveFormat waveFormat)
        {
            this.mediaType = MediaFoundationApi.CreateMediaTypeFromWaveFormat(waveFormat);
        }

        private int GetUINT32(Guid key)
        {
            int value;
            mediaType.GetUINT32(key, out value);
            return value;
        }

        public int SampleRate
        {
            get { return GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND); }
            set { mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, value); }
        }

        public int ChannelCount
        {
            get { return GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS); }
        }

        public int AverageBytesPerSecond
        {
            get { return GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND); }
        }

        public IMFMediaType MediaFoundationObject
        {
            get { return mediaType; }
        }
    }
}
