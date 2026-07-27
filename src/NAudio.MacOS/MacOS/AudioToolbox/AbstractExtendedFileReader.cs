
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the base class for extended file readers. <br />
/// Not meant to be extended by external code. <br />
/// Instead, use the dedicated subclasses of this class, 
/// located at the <see cref="NAudio.Wave"/> namespace.
/// </summary>
public abstract class AbstractExtendedFileReader : WaveStream, IDisposable
{
    private IntPtr extFileHandle;
    private WaveFormat targetFormat;
    private AudioStreamBasicDescription sourceAsbd;
    private readonly ExtendedFileReaderSettings settings;

    /// <inheritdoc />
    protected AbstractExtendedFileReader([AllowNull] ExtendedFileReaderSettings settings)
    {
        targetFormat = null;
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

        // Construct native sides, then assign client format.
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

        // If we have a client channel layout, create it and specify it.
        if (decodedSpeakers != Speakers.None)
        {
            AudioChannelLayout l = MacUtils.ConstructAudioChannelLayoutFromSpeakers(decodedSpeakers);
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

    private unsafe Speakers ComputeFileSpeakersValue(out bool needsExtensible)
    {
        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileGetPropertyInfo(extFileHandle, ExtendedAudioFileProperties.kExtAudioFileProperty_FileChannelLayout, out var layoutSize, out _)
        );
        void* clientLayoutBlock = NativeMemory.Alloc(layoutSize);
        // Zeroize the newly allocated mem block.
        Unsafe.InitBlockUnaligned(clientLayoutBlock, 0, layoutSize);
        try
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileGetProperty(
                    extFileHandle,
                    ExtendedAudioFileProperties.kExtAudioFileProperty_FileChannelLayout,
                    ref layoutSize,
                    new(clientLayoutBlock)
                )
            );
            var ret = MacUtils.ConstructSpeakersValue(new(clientLayoutBlock), out _, out needsExtensible);
            return ret;
        }
        finally
        {
            NativeMemory.Free(clientLayoutBlock);
        }
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
            DisposeReader();
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
    /// Gets the settings object provided to this instance by the 
    /// <see cref="AbstractExtendedFileReader(ExtendedFileReaderSettings)"/>
    /// constructor.
    /// </summary>
    [MaybeNull]
    public ExtendedFileReaderSettings Settings => settings;

    /// <inheritdoc />
    public sealed override bool CanRead => true;

    /// <inheritdoc />
    public sealed override bool CanSeek => true;

    /// <inheritdoc />
    public sealed override bool CanWrite => false;

    /// <inheritdoc />
    public sealed override WaveFormat WaveFormat => targetFormat;

    /// <inheritdoc />
    public sealed unsafe override int Read(Span<byte> buffer)
    {
        fixed (byte* bufferData = buffer)
        {
            uint bufferLength = (uint)buffer.Length;
            uint numFramesToRead = MacUtils.GetNumberOfPacketsFromBytesAndFormat(bufferLength, targetFormat);
            AudioBufferList list = AudioBufferList.FromSingleBuffer(
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
            return (int)MacUtils.GetNumberOfBytesFromPacketsAndFormat(numFramesToRead, targetFormat);
        }
    }

    /// <inheritdoc />
    public sealed override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <summary>
    /// Gets/sets the position of this extended file reader, expressed in number of samples. <br />
    /// You can query the exact total number of samples that the opened file contains by querying
    /// the value of the <see cref="Length"/> property.
    /// </summary>
    public sealed override long Position
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

    /// <summary>
    /// Gets the length of this extended file reader, expressed in number of samples.
    /// </summary>
    public sealed override unsafe long Length
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
    public sealed override TimeSpan CurrentTime
    {
        get
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileTell(extFileHandle, out var frameOffset)
            );
            return TimeSpan.FromSeconds(frameOffset / sourceAsbd.mSampleRate);
        }
        set => ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileSeek(extFileHandle, (long)(value.TotalSeconds * sourceAsbd.mSampleRate))
        );
    }

    /// <inheritdoc />
    public sealed override unsafe TimeSpan TotalTime
    {
        get
        {
            long totalFrames;
            uint size = sizeof(long);
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileGetProperty(
                    extFileHandle,
                    ExtendedAudioFileProperties.kExtAudioFileProperty_FileLengthFrames,
                    ref size,
                    new(&totalFrames)
                )
            );
            return TimeSpan.FromSeconds(totalFrames / sourceAsbd.mSampleRate);
        }
    }

    private void DisposeReader()
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
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { DisposeReader(); }
    }
}