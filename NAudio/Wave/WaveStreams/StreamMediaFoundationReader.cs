using System;
using System.IO;
using NAudio.MediaFoundation;

namespace NAudio.Wave
{
    /// <summary>
    /// Class for reading a stream that Media Foundation can play
    /// Will only work in Windows Vista and above
    /// Automatically converts to PCM
    /// If it is a video file with multiple audio streams, it will pick out the first audio stream
    /// </summary>
    public class StreamMediaFoundationReader : MediaFoundationReader
    {
        private readonly Stream stream;

        /// <summary>
        /// Creates a new MediaFoundationReader based on the supplied file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="settings">Advanced settings</param>
        public StreamMediaFoundationReader(Stream stream, MediaFoundationReaderSettings settings = null)
        {
            this.stream = stream;
            Init(settings);
        }

        /// <summary>
        /// Creates the reader (overridable by )
        /// </summary>
        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var ppSourceReader = MediaFoundationApi.CreateSourceReaderFromByteStream(MediaFoundationApi.CreateByteStream(new ComStream(stream, false)));

            ppSourceReader.SetStreamSelection(-2, false);
            ppSourceReader.SetStreamSelection(-3, true);
            ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, new MediaType
            {
                MajorType = MediaTypes.MFMediaType_Audio,
                SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM
            }.MediaFoundationObject);

            return ppSourceReader;
        }
    }
}
