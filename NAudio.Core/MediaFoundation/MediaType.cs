using System;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Media Type helper class, simplifying working with IMFMediaType
    /// (will probably change in the future, to inherit from an attributes class)
    /// Currently does not release the COM object, so you must do that yourself
    /// </summary>
    public class MediaType
    {
        private readonly IMFMediaType mediaType;

        /// <summary>
        /// Wraps an existing IMFMediaType object
        /// </summary>
        /// <param name="mediaType">The IMFMediaType object</param>
        public MediaType(IMFMediaType mediaType)
        {
            this.mediaType = mediaType;
        }

        /// <summary>
        /// Creates and wraps a new IMFMediaType object
        /// </summary>
        public MediaType()
        {
            mediaType = MediaFoundationApi.CreateMediaType();
        }

        /// <summary>
        /// Creates and wraps a new IMFMediaType object based on a WaveFormat
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public MediaType(WaveFormat waveFormat)
        {
            mediaType = MediaFoundationApi.CreateMediaTypeFromWaveFormat(waveFormat);
        }

        private int GetUInt32(Guid key)
        {
            int value;
            mediaType.GetUINT32(key, out value);
            return value;
        }

        private Guid GetGuid(Guid key)
        {
            Guid value;
            mediaType.GetGUID(key, out value);
            return value;
        }

        /// <summary>
        /// Tries to get a UINT32 value, returning a default value if it doesn't exist
        /// </summary>
        /// <param name="key">Attribute key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Value or default if key doesn't exist</returns>
        public int TryGetUInt32(Guid key, int defaultValue = -1)
        {
            int intValue = defaultValue;
            try
            {
                mediaType.GetUINT32(key, out intValue);
            }
            catch (COMException exception)
            {
                if (exception.GetHResult() == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
                {
                    // not a problem, return the default
                }
                else if (exception.GetHResult() == MediaFoundationErrors.MF_E_INVALIDTYPE)
                {
                    throw new ArgumentException("Not a UINT32 parameter");
                }
                else
                {
                    throw;
                }
            }
            return intValue;
        }

        /// <summary>
        /// The Sample Rate (valid for audio media types)
        /// </summary>
        public int SampleRate
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND); }
            set { mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, value); }
        }

        /// <summary>
        /// The number of Channels (valid for audio media types)
        /// </summary>
        public int ChannelCount
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS); }
            set { mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, value); }
        }

        /// <summary>
        /// The number of bits per sample (n.b. not always valid for compressed audio types)
        /// </summary>
        public int BitsPerSample
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE); }
            set { mediaType.SetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, value); }
        }

        /// <summary>
        /// The average bytes per second (valid for audio media types)
        /// </summary>
        public int AverageBytesPerSecond
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND); }
        }

        /// <summary>
        /// The Media Subtype. For audio, is a value from the AudioSubtypes class
        /// </summary>
        public Guid SubType
        {
            get { return GetGuid(MediaFoundationAttributes.MF_MT_SUBTYPE); }
            set { mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, value); }
        }

        /// <summary>
        /// The Major type, e.g. audio or video (from the MediaTypes class)
        /// </summary>
        public Guid MajorType
        {
            get { return GetGuid(MediaFoundationAttributes.MF_MT_MAJOR_TYPE); }
            set { mediaType.SetGUID(MediaFoundationAttributes.MF_MT_MAJOR_TYPE, value); }
        }

        /// <summary>
        /// Access to the actual IMFMediaType object
        /// Use to pass to MF APIs or Marshal.ReleaseComObject when you are finished with it
        /// </summary>
        public IMFMediaType MediaFoundationObject
        {
            get { return mediaType; }
        }
    }
}