
using System;
using System.IO;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the required logic to the public for the users to
/// use it as a sink to write their own audio data into - 
/// currently initialized internally and cannot be extended. <br />
/// If you want to use this class, see the methods on the <see cref="ExtendedAudioFileWriter"/> class.
/// </summary>
public abstract class AbstractExtendedFileWriter : Stream
{
    private long length;
    private bool disposed;
    private IntPtr extFileObject;
    private readonly ExtendedFileWriterSettings settings;

    private protected AbstractExtendedFileWriter(IntPtr hExtFileObject, ExtendedFileWriterSettings settings)
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        length = 0L;
        disposed = false;
        this.settings = settings;
        extFileObject = hExtFileObject;
    }

    private static AudioFormatID[] SelectBestIDForFileType(AudioFileTypeID id)
    {
        // mdcdi1315: Maybe we need to revisit this at the future to select
        // appropriate formats for all the file types. Some of these
        // for now are just plain assumptions which might not even work.
        if (
            id == AudioFileTypeIDs.kAudioFileAIFFType ||
            id == AudioFileTypeIDs.kAudioFileWAVEType ||
            id == AudioFileTypeIDs.kAudioFileBW64Type ||
            id == AudioFileTypeIDs.kAudioFileRF64Type ||
            id == AudioFileTypeIDs.kAudioFileWave64Type ||
            id == AudioFileTypeIDs.kAudioFileSoundDesigner2Type ||
            id == AudioFileTypeIDs.kAudioFileNextType ||
            id == AudioFileTypeIDs.kAudioFileCAFType
        )
        {
            return [AudioFormatIDs.kAudioFormatLinearPCM];
        }
        else if (id == AudioFileTypeIDs.kAudioFileAIFCType)
        {
            return [AudioFormatIDs.kAudioFormatAppleLossless, AudioFormatIDs.kAudioFormatMPEG4AAC];
        }
        else if (id == AudioFileTypeIDs.kAudioFileMP3Type)
        {
            return [AudioFormatIDs.kAudioFormatMPEGLayer3];
        }
        else if (id == AudioFileTypeIDs.kAudioFileMP2Type)
        {
            return [AudioFormatIDs.kAudioFormatMPEGLayer2];
        }
        else if (id == AudioFileTypeIDs.kAudioFileMP1Type)
        {
            return [AudioFormatIDs.kAudioFormatMPEGLayer1];
        }
        else if (id == AudioFileTypeIDs.kAudioFileAC3Type)
        {
            return [AudioFormatIDs.kAudioFormatAC3, AudioFormatIDs.kAudioFormat60958AC3];
        }
        else if (id == AudioFileTypeIDs.kAudioFileAAC_ADTSType)
        {
            return [AudioFormatIDs.kAudioFormatMPEG4AAC];
        }
        else if (
            id == AudioFileTypeIDs.kAudioFileMPEG4Type ||
            id == AudioFileTypeIDs.kAudioFileM4AType ||
            id == AudioFileTypeIDs.kAudioFileM4BType
        )
        {
            return [
                AudioFormatIDs.kAudioFormatMPEG4AAC_HE,
                AudioFormatIDs.kAudioFormatMPEG4AAC,
                AudioFormatIDs.kAudioFormatMPEG4AAC_LD
            ];
        }
        else if (
            id == AudioFileTypeIDs.kAudioFile3GPType ||
            id == AudioFileTypeIDs.kAudioFile3GP2Type
        )
        {
            return [
                AudioFormatIDs.kAudioFormatAppleLossless,
                AudioFormatIDs.kAudioFormatMPEG4CELP
            ];
        }
        else if (id == AudioFileTypeIDs.kAudioFileAMRType)
        {
            return [
                AudioFormatIDs.kAudioFormatiLBC,
                AudioFormatIDs.kAudioFormatAMR,
                AudioFormatIDs.kAudioFormatAMR_WB
            ];
        }
        else if (id == AudioFileTypeIDs.kAudioFileFLACType)
        {
            return [
                AudioFormatIDs.kAudioFormatFLAC,
                AudioFormatIDs.kAudioFormatOpus,
                AudioFormatIDs.kAudioFormatMPEG4AAC,
                AudioFormatIDs.kAudioFormatMPEG4AAC_HE,
                AudioFormatIDs.kAudioFormatMPEG4AAC_LD
            ];
        }
        else
        {
            throw new ArgumentException("No mapping can be found for ID " + id, nameof(id));
        }
    }

    private static AudioStreamBasicDescription GetBestFormat(ExtendedFileWriterSettings settings, out AudioFileTypeID fileTypeID)
    {
        AudioFileTypeAndFormatID ffid = new();
        foreach (var id in AudioFileLibraryInformation.GetFileTypeIDsForMimeType(settings.FileType))
        {
            foreach (var fmtId in SelectBestIDForFileType(ffid.mFileType = id))
            {
                ffid.mFormatID = fmtId;
                foreach (var asbd in AudioFileLibraryInformation.GetAvailableStreamDescriptionsForFormat(ffid))
                {
                    fileTypeID = id;
                    return asbd;
                }
            }
        }
        throw new InvalidOperationException("Could not find a suitable audio format for the specified MIME type: " + settings.FileType);
    }

    internal static AudioStreamBasicDescription BuildWriter(ExtendedFileWriterSettings settings, out AudioChannelLayout layout, out AudioFileTypeID fileTypeID)
    {
        var providingFormat = settings.OutputFormat;
        AudioStreamBasicDescription fileAsbd = GetBestFormat(settings, out fileTypeID);
        fileAsbd.mSampleRate = providingFormat.SampleRate;
        fileAsbd.mChannelsPerFrame = (uint)providingFormat.Channels;
        if (fileAsbd.mFormatID == AudioFormatIDs.kAudioFormatLinearPCM)
        {
            if (fileAsbd.mBitsPerChannel == 0U)
            {
                fileAsbd.mBitsPerChannel = (uint)providingFormat.BitsPerSample;
            }
            fileAsbd.mFramesPerPacket = 1U;
            fileAsbd.mBytesPerFrame = fileAsbd.mBytesPerPacket = (
                fileAsbd.mChannelsPerFrame * (fileAsbd.mBitsPerChannel / 8U)
            );
        }
        else
        {
            fileAsbd.mFramesPerPacket = (uint)settings.FramesPerPacket;
        }

        layout = default;

        if (providingFormat is WaveFormatExtensible ext)
        {
            var cm = ext.ChannelMask;
            if (cm != 0)
            {
                layout = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);
            }
        }

        return fileAsbd;
    }

    private protected IntPtr GetExtAudioFileHandle() => extFileObject;

    /// <summary>
    /// Gets the currently defined settings object for this extended file writer instance.
    /// </summary>
    [NotNull]
    public ExtendedFileWriterSettings Settings => settings;

    /// <inheritdoc />
    public sealed override bool CanRead => false;

    /// <inheritdoc />
    public sealed override bool CanSeek => false;

    /// <inheritdoc />
    public sealed override bool CanWrite => !disposed;

    /// <inheritdoc />
    public sealed override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return length;
        }
        set => throw new NotSupportedException("Seeking not supported on extended file writer instances");
    }

    /// <inheritdoc />
    public sealed override long Length => length;

    /// <inheritdoc />
    public sealed override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Reading not supported on extended file writer instances");
    }

    /// <inheritdoc />
    public sealed override int Read(Span<byte> buffer)
    {
        throw new NotSupportedException("Reading not supported on extended file writer instances");
    }

    /// <summary>Writes audio data to the current writer.</summary>
    /// <param name="buffer">The buffer of audio data to provide to the file writer.</param>
    public unsafe sealed override void Write(ReadOnlySpan<byte> buffer)
    {
        uint bufferLength = (uint)buffer.Length;
        var outFormat = settings.OutputFormat;
        fixed (byte* bufferPointer = buffer)
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileWrite(
                    extFileObject,
                    MacUtils.GetNumberOfPacketsFromBytesAndFormat(bufferLength, outFormat),
                    AudioBufferList.FromSingleBuffer(new(bufferPointer), bufferLength, (uint)outFormat.Channels)
                )
            );
        }
        length += bufferLength;
    }

    /// <inheritdoc />
    public sealed override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public sealed override void Flush() { }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking not supported on extended file writer instances");
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException("Can't set the length on extended file writer instances");
    }

    /// <summary>
    /// Disposes of any native data the writer has allocated. <br />
    /// Subclasses that override this should call this implementation
    /// by using the <see langword="base"/> keyword.
    /// </summary>
    protected virtual void DisposeNativeData()
    {
        if (extFileObject != IntPtr.Zero)
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileDispose(extFileObject)
            );
            extFileObject = IntPtr.Zero;
        }
    }

    /// <inheritdoc />
    protected sealed override void Dispose(bool disposing)
    {
        Monitor.Enter(this);
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
            Monitor.Exit(this);
        }
    }
}