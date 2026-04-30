using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using Interfaces = NAudio.MediaFoundation.Interfaces;

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
            MediaFoundationApi.Startup();
            Interfaces.IMFCollection availableTypes;
            try
            {
                availableTypes = MediaFoundationApi.GetAudioOutputAvailableTypes(
                    audioSubtype, MftEnumFlags.All);
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
            try
            {
                MediaFoundationException.ThrowIfFailed(availableTypes.GetElementCount(out int count));
                var mediaTypes = new List<MediaType>(count);
                for (int n = 0; n < count; n++)
                {
                    MediaFoundationException.ThrowIfFailed(availableTypes.GetElement(n, out IntPtr mediaTypePtr));
                    var mediaTypeRcw = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        mediaTypePtr, CreateObjectFlags.UniqueInstance);
                    mediaTypes.Add(new MediaType(mediaTypePtr, mediaTypeRcw));
                }
                return mediaTypes.ToArray();
            }
            finally
            {
                ((ComObject)(object)availableTypes).FinalRelease();
            }
        }

        /// <summary>
        /// Helper function to simplify encoding Window Media Audio
        /// Should be supported on Vista and above (not tested)
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .wma</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToWma(IWaveProvider inputSource, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable WMA encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputSource);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding Window Media Audio
        /// Should be supported on Vista and above (not tested)
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToWma(IWaveProvider inputSource, Stream outputStream, int desiredBitRate = 192000) {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable WMA encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                encoder.Encode(outputStream, inputSource, TranscodeContainerTypes.MFTranscodeContainerType_ASF);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to MP3
        /// By default, will only be available on Windows 8 and above
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .mp3</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToMp3(IWaveProvider inputSource, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputSource);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to MP3
        /// By default, will only be available on Windows 8 and above
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToMp3(IWaveProvider inputSource, Stream outputStream, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                encoder.Encode(outputStream, inputSource, TranscodeContainerTypes.MFTranscodeContainerType_MP3);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to AAC
        /// By default, will only be available on Windows 7 and above
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputFile">Output file path, should end with .mp4 (or .aac on Windows 8)</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToAac(IWaveProvider inputSource, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable AAC encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputSource);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to AAC
        /// By default, will only be available on Windows 7 and above
        /// </summary>
        /// <param name="inputSource">Input audio source, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToAac(IWaveProvider inputSource, Stream outputStream, int desiredBitRate = 192000) {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputSource.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable AAC encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                encoder.Encode(outputStream, inputSource, TranscodeContainerTypes.MFTranscodeContainerType_MPEG4);
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
            MediaFoundationApi.Startup();
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.SampleRate == inputFormat.SampleRate && mt.ChannelCount == inputFormat.Channels)
                .Select(mt => new { MediaType = mt, Delta = Math.Abs(desiredBitRate - mt.AverageBytesPerSecond * 8) } )
                .OrderBy(mt => mt.Delta)
                .Select(mt => mt.MediaType)
                .FirstOrDefault();
        }

        /// <summary>
        /// Default read buffer size
        /// </summary>
        public int DefaultReadBufferSize { get; set; }
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
        /// Encodes to a file. Reads directly into the MF buffer via span for zero-copy encoding.
        /// Accepts any <see cref="IWaveProvider"/> (including <see cref="WaveStream"/> subclasses).
        /// </summary>
        /// <param name="outputFile">Output filename (container type is deduced from the filename)</param>
        /// <param name="inputSource">Input audio source (should be PCM, some encoders will also allow IEEE float)</param>
        public void Encode(string outputFile, IWaveProvider inputSource)
        {
            if (inputSource.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputSource.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            using var inputMediaType = new MediaType(inputSource.WaveFormat);
            var writer = CreateSinkWriter(outputFile);
            try
            {
                MediaFoundationException.ThrowIfFailed(
                    writer.AddStream(outputMediaType.MediaFoundationObject, out int streamIndex));
                MediaFoundationException.ThrowIfFailed(
                    writer.SetInputMediaType(streamIndex, inputMediaType.MediaFoundationObject, IntPtr.Zero));
                PerformEncode(writer, streamIndex, inputSource);
            }
            finally
            {
                ((ComObject)(object)writer).FinalRelease();
            }
        }

        /// <summary>
        /// Encodes to a stream. Reads directly into the MF buffer via span for zero-copy encoding.
        /// Accepts any <see cref="IWaveProvider"/> (including <see cref="WaveStream"/> subclasses).
        /// </summary>
        /// <param name="outputStream">Output stream</param>
        /// <param name="inputSource">Input audio source (should be PCM, some encoders will also allow IEEE float)</param>
        /// <param name="transcodeContainerType">One of <see cref="TranscodeContainerTypes"/></param>
        public void Encode(Stream outputStream, IWaveProvider inputSource, Guid transcodeContainerType)
        {
            if (inputSource.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputSource.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            using var inputMediaType = new MediaType(inputSource.WaveFormat);
            var writer = CreateSinkWriter(new ComStream(outputStream), transcodeContainerType);
            try
            {
                MediaFoundationException.ThrowIfFailed(
                    writer.AddStream(outputMediaType.MediaFoundationObject, out int streamIndex));
                MediaFoundationException.ThrowIfFailed(
                    writer.SetInputMediaType(streamIndex, inputMediaType.MediaFoundationObject, IntPtr.Zero));
                PerformEncode(writer, streamIndex, inputSource);
            }
            finally
            {
                ((ComObject)(object)writer).FinalRelease();
            }
        }

        private static Interfaces.IMFSinkWriter CreateSinkWriter(string outputFile)
        {
            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension
            // n.b. AAC encode on Win 8 can have AAC extension, but use MP4 in win 7
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            Interfaces.IMFSinkWriter writer;
            var (attributesPtr, attributes) = MediaFoundationApi.CreateAttributes(1);
            try
            {
                MediaFoundationException.ThrowIfFailed(
                    attributes.SetUINT32(MediaFoundationAttributes.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1));
                try
                {
                    writer = MediaFoundationApi.CreateSinkWriterFromUrl(outputFile, IntPtr.Zero, attributesPtr);
                }
                catch (COMException e)
                {
                    if (e.GetHResult() == MediaFoundationErrors.MF_E_NOT_FOUND)
                    {
                        throw new ArgumentException("Was not able to create a sink writer for this file extension");
                    }
                    throw;
                }
            }
            finally
            {
                ((ComObject)(object)attributes).FinalRelease();
                Marshal.Release(attributesPtr);
            }
            return writer;
        }

        private static Interfaces.IMFSinkWriter CreateSinkWriter(ComStream outputStream, Guid transcodeContainerType)
        {
            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension
            // n.b. AAC encode on Win 8 can have AAC extension, but use MP4 in win 7
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            Interfaces.IMFSinkWriter writer;
            var (attributesPtr, attributes) = MediaFoundationApi.CreateAttributes(1);
            IntPtr byteStreamPtr = IntPtr.Zero;
            Interfaces.IMFByteStream byteStreamRcw = null;
            try
            {
                MediaFoundationException.ThrowIfFailed(
                    attributes.SetGUID(MediaFoundationAttributes.MF_TRANSCODE_CONTAINERTYPE, transcodeContainerType));
                (byteStreamPtr, byteStreamRcw) = MediaFoundationApi.CreateByteStream(outputStream);
                writer = MediaFoundationApi.CreateSinkWriterFromUrl(null, byteStreamPtr, attributesPtr);
            }
            finally
            {
                if (byteStreamRcw != null) ((ComObject)(object)byteStreamRcw).FinalRelease();
                if (byteStreamPtr != IntPtr.Zero) Marshal.Release(byteStreamPtr);
                ((ComObject)(object)attributes).FinalRelease();
                Marshal.Release(attributesPtr);
            }
            return writer;
        }

        private void PerformEncode(Interfaces.IMFSinkWriter writer, int streamIndex, IWaveProvider inputSource)
        {
            int bufferSize = DefaultReadBufferSize > 0
                ? DefaultReadBufferSize
                : inputSource.WaveFormat.AverageBytesPerSecond * 4;

            MediaFoundationException.ThrowIfFailed(writer.BeginWriting());

            long position = 0;
            long duration;
            do
            {
                duration = ConvertOneBuffer(writer, streamIndex, inputSource, position, bufferSize);
                position += duration;
            } while (duration > 0);

            MediaFoundationException.ThrowIfFailed(writer.DoFinalize());
        }

        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        private unsafe long ConvertOneBuffer(Interfaces.IMFSinkWriter writer, int streamIndex, IWaveProvider inputSource, long position, int bufferSize)
        {
            long durationConverted = 0;
            var (bufferPtr, buffer) = MediaFoundationApi.CreateMemoryBuffer(bufferSize);
            var (samplePtr, sample) = MediaFoundationApi.CreateSample();
            try
            {
                MediaFoundationException.ThrowIfFailed(sample.AddBuffer(bufferPtr));

                MediaFoundationException.ThrowIfFailed(buffer.Lock(out var ptr, out int maxLength, out int currentLength));
                // Read directly into the locked MF buffer via span — no intermediate managed array
                var span = new Span<byte>((void*)ptr, maxLength);
                int read = inputSource.Read(span);
                if (read > 0)
                {
                    durationConverted = BytesToNsPosition(read, inputSource.WaveFormat);
                    MediaFoundationException.ThrowIfFailed(buffer.SetCurrentLength(read));
                    MediaFoundationException.ThrowIfFailed(buffer.Unlock());
                    MediaFoundationException.ThrowIfFailed(sample.SetSampleTime(position));
                    MediaFoundationException.ThrowIfFailed(sample.SetSampleDuration(durationConverted));
                    MediaFoundationException.ThrowIfFailed(writer.WriteSample(streamIndex, samplePtr));
                }
                else
                {
                    MediaFoundationException.ThrowIfFailed(buffer.Unlock());
                }
                return durationConverted;
            }
            finally
            {
                ((ComObject)(object)sample).FinalRelease();
                Marshal.Release(samplePtr);
                ((ComObject)(object)buffer).FinalRelease();
                Marshal.Release(bufferPtr);
            }
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                outputMediaType.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
