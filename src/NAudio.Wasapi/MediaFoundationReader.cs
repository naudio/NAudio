using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Class for reading any file that Media Foundation can play
/// Will only work in Windows Vista and above
/// Automatically converts to PCM
/// If it is a video file with multiple audio streams, it will pick out the first audio stream
/// </summary>
public class MediaFoundationReader : WaveStream
{
    private WaveFormat waveFormat;
    private long length;
    private MediaFoundationReaderSettings settings;
    private readonly string file;
    private IMFSourceReader pReader;

    private long position;

    /// <summary>
    /// Allows customisation of this reader class
    /// </summary>
    public class MediaFoundationReaderSettings
    {
        /// <summary>
        /// Sets up the default settings for MediaFoundationReader
        /// </summary>
        public MediaFoundationReaderSettings()
        {
            RepositionInRead = true;
        }

        /// <summary>
        /// Allows us to request IEEE float output (n.b. no guarantee this will be accepted)
        /// </summary>
        public bool RequestFloatOutput { get; set; }
        /// <summary>
        /// If true, the reader object created in the constructor is used in Read
        /// Should only be set to true if you are working entirely on an STA thread, or
        /// entirely with MTA threads.
        /// </summary>
        public bool SingleReaderObject { get; set; }
        /// <summary>
        /// If true, the reposition does not happen immediately, but waits until the
        /// next call to read to be processed.
        /// </summary>
        public bool RepositionInRead { get; set; }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    protected MediaFoundationReader()
    {
    }

    /// <summary>
    /// Creates a new MediaFoundationReader based on the supplied file
    /// </summary>
    /// <param name="file">Filename (can also be a URL  e.g. http:// mms:// file://)</param>
    public MediaFoundationReader(string file)
        : this(file, null)
    {
    }


    /// <summary>
    /// Creates a new MediaFoundationReader based on the supplied file
    /// </summary>
    /// <param name="file">Filename</param>
    /// <param name="settings">Advanced settings</param>
    public MediaFoundationReader(string file, MediaFoundationReaderSettings settings)
    {
        this.file = file;
        Init(settings);
    }

    /// <summary>
    /// Initializes
    /// </summary>
    protected void Init(MediaFoundationReaderSettings initialSettings)
    {
        MediaFoundationApi.Startup();
        settings = initialSettings ?? new MediaFoundationReaderSettings();
        var reader = CreateReader(settings);

        waveFormat = GetCurrentWaveFormat(reader);

        MediaFoundationException.ThrowIfFailed(
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 1));
        length = GetLength(reader);

        if (settings.SingleReaderObject)
        {
            pReader = reader;
        }
        else
        {
            ((ComObject)(object)reader).FinalRelease();
        }
    }

    private WaveFormat GetCurrentWaveFormat(IMFSourceReader reader)
    {
        MediaFoundationException.ThrowIfFailed(
            reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IntPtr mediaTypePtr));
        var rcw = (IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
        using var outputMediaType = new MediaType(mediaTypePtr, rcw);

        // Two ways to query it, first is to ask for properties (second is to convert into WaveFormatEx using MFCreateWaveFormatExFromMFMediaType)
        Guid actualMajorType = outputMediaType.MajorType;
        Debug.Assert(actualMajorType == MediaTypes.MFMediaType_Audio);
        Guid audioSubType = outputMediaType.SubType;
        int channels = outputMediaType.ChannelCount;
        int bits = outputMediaType.BitsPerSample;
        int sampleRate = outputMediaType.SampleRate;

        if (audioSubType == AudioSubtypes.MFAudioFormat_PCM)
            return new WaveFormat(sampleRate, bits, channels);
        if (audioSubType == AudioSubtypes.MFAudioFormat_Float)
            return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        var subTypeDescription = FieldDescriptionHelper.Describe(typeof(AudioSubtypes), audioSubType);
        throw new InvalidDataException($"Unsupported audio sub Type {subTypeDescription}");
    }

    private static MediaType GetCurrentMediaType(IMFSourceReader reader)
    {
        MediaFoundationException.ThrowIfFailed(
            reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IntPtr mediaTypePtr));
        var rcw = (IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
        return new MediaType(mediaTypePtr, rcw);
    }

    /// <summary>
    /// Creates the reader (overridable by )
    /// </summary>
    private protected virtual IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
    {
        var reader = MediaFoundationApi.CreateSourceReaderFromUrl(file);
        MediaFoundationException.ThrowIfFailed(
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, 0));
        MediaFoundationException.ThrowIfFailed(
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 1));

        // Create a partial media type indicating that we want uncompressed PCM audio

        var partialMediaType = new MediaType();
        partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
        partialMediaType.SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

        using var currentMediaType = GetCurrentMediaType(reader);

        // mono, low sample rate files can go wrong on Windows 10 unless we specify here
        partialMediaType.ChannelCount = currentMediaType.ChannelCount;
        partialMediaType.SampleRate = currentMediaType.SampleRate;

        try
        {
            // set the media type
            // can return MF_E_INVALIDMEDIATYPE if not supported
            int hr = reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
            if (hr == MediaFoundationErrors.MF_E_INVALIDMEDIATYPE)
            {
                // HE-AAC (and v2) seems to halve the samplerate
                if (currentMediaType.SubType == AudioSubtypes.MFAudioFormat_AAC && currentMediaType.ChannelCount == 1)
                {
                    partialMediaType.SampleRate = currentMediaType.SampleRate * 2;
                    partialMediaType.ChannelCount = currentMediaType.ChannelCount * 2;
                    MediaFoundationException.ThrowIfFailed(
                        reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject));
                }
                else
                {
                    MediaFoundationException.ThrowIfFailed(hr);
                }
            }
            else
            {
                MediaFoundationException.ThrowIfFailed(hr);
            }
        }
        finally
        {
            partialMediaType.Dispose();
        }

        return reader;
    }

    private long GetLength(IMFSourceReader reader)
    {
        var variantPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
        try
        {

            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389281%28v=vs.85%29.aspx#getting_file_duration
            int hResult = reader.GetPresentationAttribute(MediaFoundationInterop.MF_SOURCE_READER_MEDIASOURCE,
                MediaFoundationAttributes.MF_PD_DURATION, variantPtr);
            if (hResult == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
            {
                // this doesn't support telling us its duration (might be streaming)
                return 0;
            }
            MediaFoundationException.ThrowIfFailed(hResult);
            var variant = Marshal.PtrToStructure<PropVariant>(variantPtr);

            var lengthInBytes = (((long)variant.Value) * waveFormat.AverageBytesPerSecond) / 10000000L;
            return lengthInBytes;
        }
        finally
        {
            PropVariant.Clear(variantPtr);
            Marshal.FreeHGlobal(variantPtr);
        }
    }

    private byte[] decoderOutputBuffer;
    private int decoderOutputOffset;
    private int decoderOutputCount;

    private void EnsureBuffer(int bytesRequired)
    {
        if (decoderOutputBuffer == null || decoderOutputBuffer.Length < bytesRequired)
        {
            decoderOutputBuffer = new byte[bytesRequired];
        }
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="offset">Offset in buffer</param>
    /// <param name="count">Bytes required</param>
    /// <returns>Number of bytes read; 0 indicates end of stream</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    private int ReadFromDecoderBuffer(Span<byte> destination)
    {
        int bytesFromDecoderOutput = Math.Min(destination.Length, decoderOutputCount);
        decoderOutputBuffer.AsSpan(decoderOutputOffset, bytesFromDecoderOutput).CopyTo(destination);
        decoderOutputOffset += bytesFromDecoderOutput;
        decoderOutputCount -= bytesFromDecoderOutput;
        if (decoderOutputCount == 0)
        {
            decoderOutputOffset = 0;
        }
        return bytesFromDecoderOutput;
    }

    /// <summary>
    /// Reads from this wave stream into a span (zero-copy path for WASAPI playback)
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        if (pReader == null)
        {
            pReader = CreateReader(settings);
        }
        if (repositionTo != -1)
        {
            Reposition(repositionTo);
        }

        int bytesWritten = 0;
        if (decoderOutputCount > 0)
        {
            bytesWritten += ReadFromDecoderBuffer(buffer);
        }

        while (bytesWritten < buffer.Length)
        {
            if (!ReadNextDecoderBuffer(out _))
            {
                break; // end of stream
            }
            bytesWritten += ReadFromDecoderBuffer(buffer.Slice(bytesWritten));
        }
        position += bytesWritten;
        return bytesWritten;
    }

    /// <summary>
    /// Pulls the next decoded sample from the source reader into <see cref="decoderOutputBuffer"/>,
    /// resetting <see cref="decoderOutputOffset"/> to 0 and <see cref="decoderOutputCount"/> to the
    /// number of decoded bytes. Returns false at end of stream (leaving the decoder buffer empty).
    /// <paramref name="timestamp"/> is the presentation time of the sample in 100-nanosecond units.
    /// </summary>
    private bool ReadNextDecoderBuffer(out long timestamp)
    {
        timestamp = 0;
        while (true)
        {
            MediaFoundationException.ThrowIfFailed(
                pReader.ReadSample(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0,
                    out int actualStreamIndex, out int dwFlagsInt, out long sampleTimestamp, out IntPtr pSamplePtr));
            var dwFlags = (SourceReaderFlags)dwFlagsInt;
            if ((dwFlags & SourceReaderFlags.EndOfStream) != 0)
            {
                if (pSamplePtr != IntPtr.Zero) Marshal.Release(pSamplePtr);
                return false;
            }
            else if ((dwFlags & SourceReaderFlags.CurrentMediaTypeChanged) != 0)
            {
                waveFormat = GetCurrentWaveFormat(pReader);
                OnWaveFormatChanged();
            }
            else if ((dwFlags & (SourceReaderFlags.StreamTick | SourceReaderFlags.NewStream
                                 | SourceReaderFlags.NativeMediaTypeChanged | SourceReaderFlags.AllEffectsRemoved)) != 0)
            {
                // Non-fatal informational flags. Per MS docs each of these can be
                // returned with a null sample; the caller should release any sample
                // pointer and call ReadSample again. Treating them as errors caused
                // legitimate gap / new-stream signals to abort the read.
                if (pSamplePtr != IntPtr.Zero) Marshal.Release(pSamplePtr);
                continue;
            }
            else if (dwFlags != 0)
            {
                if (pSamplePtr != IntPtr.Zero) Marshal.Release(pSamplePtr);
                throw new InvalidOperationException($"MediaFoundationReadError {dwFlags}");
            }

            if (pSamplePtr == IntPtr.Zero)
            {
                continue;
            }

            IMFSample pSample = null;
            IntPtr pBufferPtr = IntPtr.Zero;
            IMFMediaBuffer pBuffer = null;
            bool bufferLocked = false;
            try
            {
                pSample = (IMFSample)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(pSamplePtr, CreateObjectFlags.UniqueInstance);
                MediaFoundationException.ThrowIfFailed(pSample.ConvertToContiguousBuffer(out pBufferPtr));
                pBuffer = (IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(pBufferPtr, CreateObjectFlags.UniqueInstance);
                MediaFoundationException.ThrowIfFailed(pBuffer.Lock(out IntPtr pAudioData, out int pcbMaxLength, out int cbBuffer));
                bufferLocked = true;
                EnsureBuffer(cbBuffer);
                Marshal.Copy(pAudioData, decoderOutputBuffer, 0, cbBuffer);
                decoderOutputOffset = 0;
                decoderOutputCount = cbBuffer;
                timestamp = sampleTimestamp;
                return true;
            }
            finally
            {
                // Capture the hresult but defer throwing until every COM object has been
                // released, otherwise a failed Unlock would leak the buffer and the sample.
                int unlockHr = 0;
                if (pBuffer != null && bufferLocked)
                {
                    unlockHr = pBuffer.Unlock();
                }
                ComActivation.ReleaseBoth(pBuffer, pBufferPtr);
                ComActivation.ReleaseBoth(pSample, pSamplePtr);
                MediaFoundationException.ThrowIfFailed(unlockHr);
            }
        }
    }

    /// <summary>
    /// WaveFormat of this stream (n.b. this is after converting to PCM)
    /// </summary>
    public override WaveFormat WaveFormat
    {
        get { return waveFormat; }
    }

    /// <summary>
    /// The bytesRequired of this stream in bytes (n.b may not be accurate)
    /// </summary>
    public override long Length
    {
        get
        {
            return length;
        }
    }

    /// <summary>
    /// Current position within this stream
    /// </summary>
    public override long Position
    {
        get { return position; }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", "Position cannot be less than 0");
            if (settings.RepositionInRead)
            {
                repositionTo = value;
                position = value; // for gui apps, make it look like we have alread processed the reposition
            }
            else
            {
                Reposition(value);
            }
        }
    }

    private long repositionTo = -1;

    private void Reposition(long desiredPosition)
    {
        if (pReader == null)
        {
            pReader = CreateReader(settings);
        }

        int averageBytesPerSecond = waveFormat.AverageBytesPerSecond;
        long nsPosition = averageBytesPerSecond > 0
            ? (10000000L * desiredPosition) / averageBytesPerSecond
            : 0;
        var pv = PropVariant.FromLong(nsPosition);
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pv));
        try
        {
            Marshal.StructureToPtr(pv, ptr, false);

            // should pass in a variant of type VT_I8 which is a long containing time in 100nanosecond units
            MediaFoundationException.ThrowIfFailed(pReader.SetCurrentPosition(Guid.Empty, ptr));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        decoderOutputCount = 0;
        decoderOutputOffset = 0;
        repositionTo = -1;// clear the flag

        // IMFSourceReader.SetCurrentPosition is documented as inexact: it seeks to the nearest
        // container keyframe at or before the requested time, so the next decoded sample can begin
        // earlier than desiredPosition. Per MS guidance we must read forward and skip into the
        // decoded buffer to land on the exact byte position; without this, playback audibly
        // restarts from the keyframe (very visible on audio extracted from video - Vorbis/AAC).
        // (We can only do this when the byte rate is known; streaming sources report 0.)
        long achievedPosition = desiredPosition;
        if (averageBytesPerSecond > 0)
        {
            while (true)
            {
                if (!ReadNextDecoderBuffer(out long timestamp))
                {
                    // Ran past the end of the stream while seeking; leave the decoder buffer empty
                    // so the next Read reports end-of-stream, and snap position to the known length.
                    decoderOutputCount = 0;
                    decoderOutputOffset = 0;
                    achievedPosition = length > 0 ? length : desiredPosition;
                    break;
                }

                // The decoder output is PCM (constant bit rate), so the sample timestamp maps to an
                // exact byte position. MF can return several buffers carrying the same timestamp, so
                // we gate on the end of the buffer rather than its start.
                long decoderPosition = (long)((ulong)timestamp * (ulong)averageBytesPerSecond / 10000000UL);
                long bufferEnd = decoderPosition + decoderOutputCount;

                if (bufferEnd <= desiredPosition)
                {
                    // Entire buffer is still before the target; discard it and keep reading.
                    decoderOutputCount = 0;
                    decoderOutputOffset = 0;
                    continue;
                }

                if (decoderPosition < desiredPosition)
                {
                    int skip = (int)(desiredPosition - decoderPosition);
                    // Round the skip down to a frame boundary; starting mid-sample produces loud
                    // static. This lands us at most BlockAlign-1 bytes before the requested position.
                    skip -= skip % BlockAlign;
                    decoderOutputOffset = skip;
                    decoderOutputCount -= skip;
                    achievedPosition = decoderPosition + skip;
                }
                else
                {
                    // Keyframe landed at or after the request; deliver the buffer from its start.
                    achievedPosition = decoderPosition;
                }
                break;
            }
        }

        position = achievedPosition;
    }

    /// <summary>
    /// Cleans up after finishing with this reader
    /// </summary>
    /// <param name="disposing">true if called from Dispose</param>
    protected override void Dispose(bool disposing)
    {
        if (pReader != null)
        {
            ((ComObject)(object)pReader).FinalRelease();
            pReader = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// WaveFormat has changed
    /// </summary>
    public event EventHandler WaveFormatChanged;

    private void OnWaveFormatChanged()
    {
        WaveFormatChanged?.Invoke(this, EventArgs.Empty);
    }
}
