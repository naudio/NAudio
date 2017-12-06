using System;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Windows.Storage.Streams;

namespace NAudioUniversalDemo
{
    // Slightly hacky approach to supporting a different WinRT constructor
    class MediaFoundationReaderUniversal : MediaFoundationReader
    {
        private readonly MediaFoundationReaderUniversalSettings settings;

        public class MediaFoundationReaderUniversalSettings : MediaFoundationReaderSettings
        {
            public MediaFoundationReaderUniversalSettings()
            {
                // can't recreate since we're using a file stream
                SingleReaderObject = true;
            }

            public IRandomAccessStream Stream { get; set; }
        }

        public MediaFoundationReaderUniversal(IRandomAccessStream stream)
            : this(new MediaFoundationReaderUniversalSettings() {Stream = stream})
        {
            
        }
        

        public MediaFoundationReaderUniversal(MediaFoundationReaderUniversalSettings settings)
            : base(null, settings)
        {
            this.settings = settings;
        }

        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var fileStream = ((MediaFoundationReaderUniversalSettings) settings).Stream;
            var byteStream = MediaFoundationApi.CreateByteStream(fileStream);
            var reader = MediaFoundationApi.CreateSourceReaderFromByteStream(byteStream);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

            // Create a partial media type indicating that we want uncompressed PCM audio

            var partialMediaType = new MediaType();
            partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
            partialMediaType.SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

            // set the media type
            // can return MF_E_INVALIDMEDIATYPE if not supported
            reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
            return reader;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                settings.Stream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}