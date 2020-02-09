using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.MediaFoundation;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Media Foundation Encoder class allows you to use Media Foundation to encode an IWaveProvider
    /// to any supported encoding format
    /// </summary>
    public class MediaFoundationEncoder : IDisposable
    {
        /// <summary>
        /// Queries the available bitrates for a given encoding output type, sample rate and number of channels
        /// </summary>
        /// <param name="audioSubtype">Audio subtype - a value from the AudioSubtypes class</param>
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

        /// <summary>
        /// Gets all the available media types for a particular 
        /// </summary>
        /// <param name="audioSubtype">Audio subtype - a value from the AudioSubtypes class</param>
        /// <returns>An array of available media types that can be encoded with this subtype</returns>
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
                if (c.GetHResult() == MediaFoundationErrors.MF_E_NOT_FOUND)
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

        /// <summary>
        /// Helper function to simplify encoding Window Media Audio
        /// Should be supported on Vista and above (not tested)
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .wma</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToWma(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable WMA encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to MP3
        /// By default, will only be available on Windows 8 and above
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .mp3</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToMp3(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to AAC
        /// By default, will only be available on Windows 7 and above
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .mp4 (or .aac on Windows 8)</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToAac(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            // Information on configuring an AAC media type can be found here:
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable AAC encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                // should AAC container have ADTS, or is that just for ADTS?
                // http://www.hydrogenaudio.org/forums/index.php?showtopic=97442
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Tries to find the encoding media type with the closest bitrate to that specified
        /// </summary>
        /// <param name="audioSubtype">Audio subtype, a value from AudioSubtypes</param>
        /// <param name="inputFormat">Your encoder input format (used to check sample rate and channel count)</param>
        /// <param name="desiredBitRate">Your desired bitrate</param>
        /// <returns>The closest media type, or null if none available</returns>
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

        /// <summary>
        /// Creates a new encoder that encodes to the specified output media type
        /// </summary>
        /// <param name="outputMediaType">Desired output media type</param>
        public MediaFoundationEncoder(MediaType outputMediaType)
        {
            if (outputMediaType == null) throw new ArgumentNullException("outputMediaType");
            this.outputMediaType = outputMediaType;
        }

        /// <summary>
        /// Encodes a file
        /// </summary>
        /// <param name="outputFile">Output filename (container type is deduced from the filename)</param>
        /// <param name="inputProvider">Input provider (should be PCM, some encoders will also allow IEEE float)</param>
        public void Encode(string outputFile, IWaveProvider inputProvider)
        {
            if (inputProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            var inputMediaType = new MediaType(inputProvider.WaveFormat);

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
                if (e.GetHResult() == MediaFoundationErrors.MF_E_NOT_FOUND)
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
            IMFMediaBuffer buffer = MediaFoundationApi.CreateMemoryBuffer(managedBuffer.Length);
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

        /// <summary>
        /// Disposes this instance
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            Marshal.ReleaseComObject(outputMediaType.MediaFoundationObject);
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MediaFoundationEncoder()
        {
            Dispose(false);
        }
    }
}
