using System;
using System.IO;
using NAudio.MediaFoundation;

namespace NAudio.Wave
{
    public class StreamMediaFoundationReader : MediaFoundationReader
    {
        private readonly Stream stream;

        public StreamMediaFoundationReader(Stream stream, MediaFoundationReaderSettings settings = null)
        {
            this.stream = stream;
            Init(settings);
        }

        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var ppSourceReader = MediaFoundationApi.CreateSourceReaderFromByteStream(MediaFoundationApi.CreateByteStream(new ComStream(stream)));

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
