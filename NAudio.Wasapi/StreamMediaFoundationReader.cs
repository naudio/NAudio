using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.MediaFoundation;
using Interfaces = NAudio.MediaFoundation.Interfaces;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// MediaFoundationReader supporting reading from a stream
    /// </summary>
    public class StreamMediaFoundationReader : MediaFoundationReader
    {
        private readonly Stream stream;

        /// <summary>
        /// Constructs a new media foundation reader from a stream
        /// </summary>
        public StreamMediaFoundationReader(Stream stream, MediaFoundationReaderSettings settings = null)
        {
            this.stream = stream;
            Init(settings);
        }

        /// <summary>
        /// Creates the reader
        /// </summary>
        private protected override Interfaces.IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var (byteStreamPtr, byteStreamRcw) = MediaFoundationApi.CreateByteStream(new ComStream(stream));
            try
            {
                var reader = MediaFoundationApi.CreateSourceReaderFromByteStream(byteStreamPtr);

                MediaFoundationException.ThrowIfFailed(
                    reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, 0));
                MediaFoundationException.ThrowIfFailed(
                    reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 1));

                using var partialMediaType = new MediaType
                {
                    MajorType = MediaTypes.MFMediaType_Audio,
                    SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM
                };
                MediaFoundationException.ThrowIfFailed(
                    reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM,
                        IntPtr.Zero, partialMediaType.MediaFoundationObject));

                return reader;
            }
            finally
            {
                // Source reader AddRef'd the byte stream internally; we can drop our refs.
                ((ComObject)(object)byteStreamRcw).FinalRelease();
                Marshal.Release(byteStreamPtr);
            }
        }
    }
}
