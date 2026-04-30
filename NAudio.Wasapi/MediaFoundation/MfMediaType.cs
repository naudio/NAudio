using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFMediaType providing managed access to media type attributes.
    /// </summary>
    public class MfMediaType : IDisposable
    {
        private Interfaces.IMFMediaType mediaTypeInterface;
        private IntPtr nativePointer;

        internal MfMediaType(Interfaces.IMFMediaType mediaTypeInterface, IntPtr nativePointer)
        {
            this.mediaTypeInterface = mediaTypeInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Gets the native COM pointer for this media type (for passing to other COM methods).
        /// </summary>
        internal IntPtr NativePointer => nativePointer;

        /// <summary>
        /// Gets the major type GUID (e.g. audio, video).
        /// </summary>
        public Guid MajorType
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(mediaTypeInterface.GetMajorType(out var majorType));
                return majorType;
            }
        }

        /// <summary>
        /// Gets or sets the subtype GUID (e.g. PCM, AAC).
        /// </summary>
        public Guid SubType
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(mediaTypeInterface.GetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, out var subType));
                return subType;
            }
            set => MediaFoundationException.ThrowIfFailed(mediaTypeInterface.SetGUID(MediaFoundationAttributes.MF_MT_SUBTYPE, value));
        }

        /// <summary>
        /// Gets whether this is a compressed format.
        /// </summary>
        public bool IsCompressed
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(mediaTypeInterface.IsCompressedFormat(out var compressed));
                return compressed != 0;
            }
        }

        /// <summary>
        /// Gets or sets a UINT32 attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The attribute value.</returns>
        public int GetUInt32(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.GetUINT32(key, out var value));
            return value;
        }

        /// <summary>
        /// Tries to get a UINT32 attribute, returning a default value if it doesn't exist.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <param name="defaultValue">Default value if attribute not found.</param>
        /// <returns>The attribute value, or defaultValue if not found.</returns>
        public int TryGetUInt32(Guid key, int defaultValue = -1)
        {
            int hr = mediaTypeInterface.GetUINT32(key, out var value);
            if (hr == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
                return defaultValue;
            MediaFoundationException.ThrowIfFailed(hr);
            return value;
        }

        /// <summary>
        /// Sets a UINT32 attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <param name="value">The attribute value.</param>
        public void SetUInt32(Guid key, int value)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.SetUINT32(key, value));
        }

        /// <summary>
        /// Gets a UINT64 attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The attribute value.</returns>
        public long GetUInt64(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.GetUINT64(key, out var value));
            return value;
        }

        /// <summary>
        /// Sets a UINT64 attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <param name="value">The attribute value.</param>
        public void SetUInt64(Guid key, long value)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.SetUINT64(key, value));
        }

        /// <summary>
        /// Gets a GUID attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The attribute value.</returns>
        public Guid GetGuid(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.GetGUID(key, out var value));
            return value;
        }

        /// <summary>
        /// Sets a GUID attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <param name="value">The attribute value.</param>
        public void SetGuid(Guid key, Guid value)
        {
            MediaFoundationException.ThrowIfFailed(mediaTypeInterface.SetGUID(key, value));
        }

        /// <summary>
        /// Gets or sets the sample rate (valid for audio media types).
        /// </summary>
        public int SampleRate
        {
            get => GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND);
            set => SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, value);
        }

        /// <summary>
        /// Gets or sets the number of channels (valid for audio media types).
        /// </summary>
        public int ChannelCount
        {
            get => GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS);
            set => SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, value);
        }

        /// <summary>
        /// Gets or sets the number of bits per sample (valid for audio media types).
        /// </summary>
        public int BitsPerSample
        {
            get => GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE);
            set => SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, value);
        }

        /// <summary>
        /// Gets the average bytes per second (valid for audio media types).
        /// </summary>
        public int AverageBytesPerSecond => GetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND);

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (mediaTypeInterface != null)
            {
                ((ComObject)(object)mediaTypeInterface).FinalRelease();
                mediaTypeInterface = null;
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
