
using System;
using System.Numerics;
using System.Threading;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the base class for extended audio file services readers. <br />
/// Not meant to be extended by external code. <br />
/// Instead, use the dedicated subclasses of this class, 
/// located at the <see cref="NAudio.Wave"/> namespace.
/// </summary>
[SupportedOSPlatform("ios2.1")]
[SupportedOSPlatform("macos10.4")]
public abstract class ExtendedAudioFileServicesReader : WaveStream
{
    private bool disposed;
    private long clientReadBytes;
    private IntPtr extFileHandle;
    private WaveFormat targetFormat;
    private readonly object lockObject;
    private AudioStreamBasicDescription sourceAsbd;
    private readonly ExtendedAudioFileReaderSettings settings;

    private protected ExtendedAudioFileServicesReader([AllowNull] ExtendedAudioFileReaderSettings settings)
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        disposed = false;
        lockObject = new();
        targetFormat = null;
        clientReadBytes = 0L;
        this.settings = settings;
    }

    private unsafe void InitReaderInternal()
    {
        uint size = (uint)sizeof(AudioStreamBasicDescription);
        // Get the file's stream description
        // Will be needed for various API's.
        fixed (AudioStreamBasicDescription* asbdPointer = &sourceAsbd)
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileGetProperty(extFileHandle, ExtendedAudioFileProperties.kExtAudioFileProperty_FileDataFormat, ref size, new(asbdPointer))
            );
        }

        // Construct target wave format
        // The wave format is constructed with minimal effort 
        // closest to the file's one, if no special options were given by the user.

        bool allowNonPowerOfTwoRates = true;
        bool needsIeeeFloatSpecifically = false;
        Speakers decodedSpeakers = Speakers.None;

        if (settings is not null)
        {
            targetFormat = settings.OutputFormat;
            if (targetFormat is null)
            {
                needsIeeeFloatSpecifically = settings.RequestIeeeFloat;
                allowNonPowerOfTwoRates = settings.AllowNonPowerOfTwoBitRates;
            }
            else if (targetFormat is WaveFormatExtensible ext)
            {
                decodedSpeakers = (Speakers)ext.ChannelMask;
            }
        }

        if (targetFormat is null)
        {
            // Source format may be wildly incomplete, so try to complete it and do validation on it.
            bool bitRateSpecified = false;
            int bitRate, sampleRate, channels;
            if (needsIeeeFloatSpecifically)
            {
                bitRate = 32;
            }
            // Bit rate may be specified as zero, so define good defaults for it.
            else if (sourceAsbd.mBitsPerChannel == 0U)
            {
                // 16 bit depth suffices for most cases.
                bitRate = 16;
            }
            else
            {
                // The bit rate might not be a power of two for some files,
                // so we need to flag that to not lose any information 
                // (if of course the user wants non-power of two bit depth rates)
                bitRateSpecified = !allowNonPowerOfTwoRates;
                bitRate = (int)sourceAsbd.mBitsPerChannel;
            }

            if (sourceAsbd.mSampleRate == 0d)
            {
                // If we have a sample rate of zero we cannot assume nothing about the file itself;
                // Probably the API falsely reached up to this point, so we throw.
                throw new InvalidOperationException("Invalid file was provided to the reader; Reader reported 0 sample rate!");
            }
            else
            {
                // This conversion to (int) also helps us to avoid having in the output format
                // a sample rate that could be a fractional value, which the WaveFormat API today does not support.
                sampleRate = (int)sourceAsbd.mSampleRate;
            }

            if (sourceAsbd.mChannelsPerFrame == 0U)
            {
                // If we have a channel count of zero we cannot assume nothing about the file itself;
                // Probably the API falsely reached up to this point, so we throw.
                throw new InvalidOperationException("Invalid file was provided to the reader; Reader reported 0 audible channels!");
            }
            else
            {
                channels = (int)sourceAsbd.mChannelsPerFrame;
            }

            // Query for the speakers now.
            bool extensibleIsNeeded = false;
            decodedSpeakers = ComputeFileSpeakersValue(out extensibleIsNeeded);

            if (extensibleIsNeeded || (bitRateSpecified && (!BitOperations.IsPow2(bitRate))))
            {
                targetFormat = new WaveFormatExtensible(
                    sampleRate,
                    bitRateSpecified ? (int)BitOperations.RoundUpToPowerOf2((uint)bitRate) : bitRate,
                    channels,
                    needsIeeeFloatSpecifically,
                    bitRate,
                    decodedSpeakers
                );
            }
            else
            {
                targetFormat = needsIeeeFloatSpecifically ?
                    WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels) :
                    new(sampleRate, bitRate, channels);
            }
        }

        // Reinterpret the target format as a macOS ASBD structure.
        AudioStreamBasicDescription asbdOut = MacUtils.ConstructASBDFromWaveFormat(targetFormat);

        size = (uint)sizeof(AudioStreamBasicDescription);
        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileSetProperty(
                extFileHandle,
                ExtendedAudioFileProperties.kExtAudioFileProperty_ClientDataFormat,
                size,
                new(&asbdOut)
            )
        );

        // If we have a decoded speakers value, 
        // create a channel layout for it and specify it
        // as the client channel layout.
        if (decodedSpeakers != Speakers.None)
        {
            var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers(decodedSpeakers);
            size = (uint)sizeof(AudioChannelLayout);
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileSetProperty(
                    extFileHandle,
                    ExtendedAudioFileProperties.kExtAudioFileProperty_ClientChannelLayout,
                    size,
                    new(&l)
                )
            );
        }
    }

    private Speakers ComputeFileSpeakersValue(out bool needsExtensible)
    {
        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileGetPropertyInfo(
                extFileHandle,
                ExtendedAudioFileProperties.kExtAudioFileProperty_FileChannelLayout,
                out var layoutSize,
                out _
            )
        );
        using var handle = ChannelLayoutHandle.Allocate(layoutSize);
        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileGetProperty(
                extFileHandle,
                ExtendedAudioFileProperties.kExtAudioFileProperty_FileChannelLayout,
                ref layoutSize,
                handle.DangerousGetHandle()
            )
        );
        return MacUtils.ConstructSpeakersValue(handle.DangerousGetHandle(), out _, out needsExtensible);
    }

    /// <summary>
    /// Initializes the reader. <br />
    /// This should typically be called inside the constructor.
    /// </summary>
    protected void Init()
    {
        try
        {
            extFileHandle = InitializeReader();
            InitReaderInternal();
        }
        catch
        {
            DisposeNativeData();
            disposed = true;
            throw;
        }
    }

    /// <summary>
    /// Implementations of this allow to initialize the 
    /// Extended Audio File services from a source.
    /// </summary>
    /// <returns>Handle to an extended audio file.</returns>
    protected abstract IntPtr InitializeReader();

    /// <summary>
    /// Gets the settings object provided to this instance
    /// during construction.
    /// </summary>
    [MaybeNull]
    public ExtendedAudioFileReaderSettings Settings => settings;

    /// <inheritdoc />
    public sealed override bool CanRead => !disposed;

    /// <inheritdoc />
    public sealed override bool CanSeek => !disposed;

    /// <inheritdoc />
    public sealed override bool CanWrite => false;

    /// <inheritdoc />
    public sealed override WaveFormat WaveFormat => targetFormat;

    /// <summary>
    /// Reads audio data and places them to the specified <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">
    /// A region of memory. 
    /// When this method returns, the contents of this region are 
    /// replaced by the bytes read from the current reader.
    /// </param>
    /// <returns><inheritdoc /></returns>
    /// <remarks>
    /// Note that this method is not thread-safe; 
    /// You can read from a different thread from where the reader
    /// was created from, but you must ensure that this
    /// call is only performed by one specific thread.
    /// </remarks>
    public sealed override unsafe int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        uint bufferLength = (uint)buffer.Length;
        uint numFramesToRead = MacUtils.GetNumberOfPacketsFromBytesAndFormat(bufferLength, targetFormat);
        if (numFramesToRead == 0U && bufferLength > 0U)
        {
            // We have a value less than BlockAlign.
            // Throw to avoid such subtle issues.
            throw new ArgumentException("Buffer length cannot be less than the stream's block alignment.", nameof(buffer));
        }
        fixed (byte* bufferData = buffer)
        {
            var list = AudioBufferList.FromSingleBuffer(
                new(bufferData),
                bufferLength,
                (uint)targetFormat.Channels
            );
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileRead(
                    extFileHandle,
                    ref numFramesToRead,
                    ref list
                )
            );
        }
        int readBytes = (int)MacUtils.GetNumberOfBytesFromPacketsAndFormat(numFramesToRead, targetFormat);
        // Even if readBytes is zero, we will just add zero, which won't modify the clientReadBytes value.
        clientReadBytes += readBytes;
        return readBytes;
    }

    /// <inheritdoc />
    public sealed override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <summary>
    /// Gets the position, in bytes, of this extended audio file reader.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This reader instance has been disposed of.</exception>
    public sealed override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return clientReadBytes;
        }
        set
        {
            // The getter checks this and the property is documented as throwing
            // it, but the setter went straight to the native API with a dead
            // handle and surfaced a bare kAudio_ParamError instead.
            ObjectDisposedException.ThrowIf(disposed, this);
            // We could validate the value against the Length property,
            // but that is just an estimate, and so could falsely flag
            // ArgumentOutOfRangeException while there are still audio data to return.
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "New position cannot be less than zero.");
            }
            else
            {
                // Make sure that the input position is in a BlockAlign multiple.
                value -= (value % targetFormat.BlockAlign);
                // Calculate the new position: divide the input value by AverageBytesPerSecond
                // of the target format to get a value in seconds, then multiply by the file's
                // sample rate to get the actual position to seek to, expressed in the
                // file's sample frames.
                long newPos = (long)((value / (double)targetFormat.AverageBytesPerSecond) * sourceAsbd.mSampleRate);
                long length = LengthInFrames;
                // Because the above calculation is just an estimated value,
                // that means that newPos could be much larger than length.
                // To fix this, we just bound to length so that no out of 
                // range exception is thrown.
                if (newPos > length) { newPos = length; }
                PositionInFrames = newPos;
                // Make sure that we set this value after seeking succeeded.
                clientReadBytes = value;
            }
        }
    }

    /// <summary>
    /// Gets the length, in bytes, of this extended audio file reader.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This reader instance has been disposed of.</exception>
    public sealed override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            long length = (long)((LengthInFrames / sourceAsbd.mSampleRate) * targetFormat.AverageBytesPerSecond);
            // The reader provides to us the length in sample frames of the file format,
            // not on the format that we resample. Although the above calculation is accurate,
            // if the file's sample rate is fractional, this fails because the number of
            // frames returned by the API is always integer-bound. The simple fix is to 
            // assume that the length is accurate by definition, so if a non-block
            // aligned value is given to us by the above calculation, we assume that
            // there is an additional frame that was given to us, so we add a frame.
            long drift = length % targetFormat.BlockAlign;
            if (drift > 0L) { length += targetFormat.BlockAlign - drift; }
            return length;
        }
    }

    private long PositionInFrames
    {
        get
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileTell(extFileHandle, out var frameOffset)
            );
            return frameOffset;
        }
        set => ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileSeek(extFileHandle, value)
        );
    }

    private unsafe long LengthInFrames
    {
        get
        {
            long value;
            uint size = sizeof(long);
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileGetProperty(
                    extFileHandle,
                    ExtendedAudioFileProperties.kExtAudioFileProperty_FileLengthFrames,
                    ref size,
                    new(&value)
                )
            );
            return value;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">This reader instance has been disposed of.</exception>
    // CurrentTime is sample-accurate: This happens because we use PositionInFrames and the file's sample 
    // rate to deduce the TimeSpan and vice-versa, so the conversion is straightforward and always 
    // produces accurate results.
    public sealed override TimeSpan CurrentTime
    {
        get => TimeSpan.FromSeconds(PositionInFrames / sourceAsbd.mSampleRate);
        set => PositionInFrames = (long)(value.TotalSeconds * sourceAsbd.mSampleRate);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">This reader instance has been disposed of.</exception>
    public sealed override TimeSpan TotalTime
        => TimeSpan.FromSeconds(LengthInFrames / sourceAsbd.mSampleRate);

    /// <summary>
    /// Disposes of any native data the reader has allocated. <br />
    /// Subclasses that override this should call this implementation
    /// by using the <see langword="base"/> keyword.
    /// </summary>
    protected virtual void DisposeNativeData()
    {
        if (extFileHandle != IntPtr.Zero)
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileDispose(extFileHandle)
            );
            extFileHandle = IntPtr.Zero;
        }
    }

    /// <inheritdoc />
    protected sealed override void Dispose(bool disposing)
    {
        Monitor.Enter(lockObject);
        try
        {
            base.Dispose(disposing);
            if (disposing && (!disposed))
            {
                DisposeNativeData();
                disposed = true;
            }
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
    }
}