using System;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Windows.Storage.Streams;

namespace NAudioWin8Demo
{
    // Slightly hacky approach to supporting a different WinRT constructor
    class MediaFoundationReaderRT : MediaFoundationReader
    {
        private readonly MediaFoundationReaderRTSettings settings;

        public class MediaFoundationReaderRTSettings : MediaFoundationReaderSettings
        {
            public MediaFoundationReaderRTSettings()
            {
                // can't recreate since we're using a file stream
                this.SingleReaderObject = true;
            }

            public IRandomAccessStream Stream { get; set; }
        }

        public MediaFoundationReaderRT(IRandomAccessStream stream)
            : this(new MediaFoundationReaderRTSettings() {Stream = stream})
        {
            
        }
        

        public MediaFoundationReaderRT(MediaFoundationReaderRTSettings settings)
            : base(null, settings)
        {
            this.settings = settings;
        }

        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var fileStream = ((MediaFoundationReaderRTSettings) settings).Stream;
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
            if (disposing && settings.Stream != null)
            {
                settings.Stream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}