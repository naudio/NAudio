using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Media Type helper class, simplifying working with IMFMediaType.
    /// Implements IDisposable for proper COM lifetime management.
    /// </summary>
    public class MediaType : IDisposable
    {
        private Interfaces.IMFMediaType mediaType;
        private IntPtr nativePointer;

        /// <summary>
        /// Wraps an existing IMFMediaType object. Caller transfers ownership of both refs.
        /// </summary>
        /// <param name="ptr">The native COM pointer (one ref).</param>
        /// <param name="rcw">The source-generated RCW for the same object (one ref).</param>
        internal MediaType(IntPtr ptr, Interfaces.IMFMediaType rcw)
        {
            this.nativePointer = ptr;
            this.mediaType = rcw;
        }

        /// <summary>
        /// Creates and wraps a new IMFMediaType object
        /// </summary>
        public MediaType()
        {
            (nativePointer, mediaType) = MediaFoundationApi.CreateMediaType();
        }

        /// <summary>
        /// Creates and wraps a new IMFMediaType object based on a WaveFormat
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public MediaType(WaveFormat waveFormat)
        {
            (nativePointer, mediaType) = MediaFoundationApi.CreateMediaTypeFromWaveFormat(waveFormat);
        }

        private int GetUInt32(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(mediaType.GetUINT32(key, out int value));
            return value;
        }

        private Guid GetGuid(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(mediaType.GetGUID(key, out Guid value));
            return value;
        }

        /// <summary>
        /// Tries to get a UINT32 value, returning a default value if it doesn't exist
        /// </summary>
        public int TryGetUInt32(Guid key, int defaultValue = -1)
        {
            int hr = mediaType.GetUINT32(key, out int intValue);
            if (hr == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
            {
                return defaultValue;
            }
            if (hr == MediaFoundationErrors.MF_E_INVALIDTYPE)
            {
                throw new ArgumentException("Not a UINT32 parameter");
            }
            MediaFoundationException.ThrowIfFailed(hr);
            return intValue;
        }

        /// <summary>
        /// Sets a UINT32 attribute on this media type
        /// </summary>
        public void SetUInt32(Guid key, int value)
        {
            MediaFoundationException.ThrowIfFailed(mediaType.SetUINT32(key, value));
        }

        /// <summary>
        /// The Sample Rate (valid for audio media types)
        /// </summary>
        public int SampleRate
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND); }
            set { SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, value); }
        }

        /// <summary>
        /// The number of Channels (valid for audio media types)
        /// </summary>
        public int ChannelCount
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS); }
            set { SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, value); }
        }

        /// <summary>
        /// The number of bits per sample (n.b. not always valid for compressed audio types)
        /// </summary>
        public int BitsPerSample
        {
            get { return GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE); }
            set { SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, value); }
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
            set { MediaFoundationException.ThrowIfFailed(mediaType.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, value)); }
        }

        /// <summary>
        /// The Major type, e.g. audio or video (from the MediaTypes class)
        /// </summary>
        public Guid MajorType
        {
            get { return GetGuid(MediaFoundationAttributes.MF_MT_MAJOR_TYPE); }
            set { MediaFoundationException.ThrowIfFailed(mediaType.SetGUID(MediaFoundationAttributes.MF_MT_MAJOR_TYPE, value)); }
        }

        /// <summary>
        /// Gets the number of attributes set on this media type.
        /// </summary>
        public int AttributeCount
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(mediaType.GetCount(out int count));
                return count;
            }
        }

        /// <summary>
        /// Retrieves an attribute at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index of the attribute</param>
        /// <param name="key">Receives the attribute GUID key</param>
        /// <param name="valuePtr">Receives the attribute value as a PropVariant. Caller must free with PropVariant.Clear.</param>
        public void GetAttributeByIndex(int index, out Guid key, IntPtr valuePtr)
        {
            MediaFoundationException.ThrowIfFailed(mediaType.GetItemByIndex(index, out key, valuePtr));
        }

        /// <summary>
        /// Native COM pointer for the underlying IMFMediaType, for passing to APIs that take an
        /// IntPtr-typed media-type parameter (e.g. <c>IMFTransform::SetInputType</c>,
        /// <c>IMFSourceReader::SetCurrentMediaType</c>, <c>IMFSinkWriter::AddStream</c>).
        /// For internal use - callers should use the wrapper properties instead.
        /// </summary>
        internal IntPtr MediaFoundationObject => nativePointer;

        /// <summary>
        /// Releases the underlying COM object.
        /// </summary>
        public void Dispose()
        {
            if (mediaType != null)
            {
                ((ComObject)(object)mediaType).FinalRelease();
                mediaType = null;
            }
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
