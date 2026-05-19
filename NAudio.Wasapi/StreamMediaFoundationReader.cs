using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.MediaFoundation;
using NAudio.MediaFoundation.Interfaces;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// MediaFoundationReader supporting reading from a stream <br />
    /// Will only work in Windows Vista and above. <br />
    /// Automatically converts to PCM. <br />
    /// If it is a video file with multiple audio streams, it will pick out the first audio stream.
    /// </summary>
    public class StreamMediaFoundationReader : MediaFoundationReader
    {
        private readonly Stream stream;
        private readonly MFByteStreamFromStream wrapper;

        /// <summary>
        /// Constructs a new media foundation reader from a stream
        /// </summary>
        /// <param name="stream">The data stream to initialize the reader from.</param>
        /// <param name="settings">Optional. Additional options that affect how the reader reads data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is unreadable, or unseekable.</exception>
        public StreamMediaFoundationReader(Stream stream, MediaFoundationReaderSettings settings = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }
            else if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(stream));
            }
            else
            {
                wrapper = new MFByteStreamFromStream(this.stream = stream);
                Init(settings);
            }
        }

        /// <summary>
        /// Creates the reader
        /// </summary>
        private protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            IntPtr byteStreamPtr = MediaFoundationApi.CreateByteStream(wrapper);
            try
            {
                wrapper.ResetPosition();
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
                // Source reader AddRef'd the byte stream internally; we drop our ref.
                Marshal.Release(byteStreamPtr);
            }
        }

        /// <summary>
        /// Cleanup the wrapper implementation
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    // Now we know that the reader is disposed, so we can dispose our wrapper and the stream as well.
                    // Also serialize access here.
                    System.Threading.Monitor.Enter(this);
                    try
                    {
                        wrapper.Dispose();
                    }
                    finally
                    {
                        try
                        {
                            stream.Dispose();
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(this);
                        }
                    }
                }
            }
        }
    }
}
