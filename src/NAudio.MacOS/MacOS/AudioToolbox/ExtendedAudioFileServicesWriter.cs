
using System;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Collections.Immutable;
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
[SupportedOSPlatform("ios2.1")]
[SupportedOSPlatform("macos10.4")]
public abstract class ExtendedAudioFileServicesWriter : Stream
{
    private long length;
    private bool disposed;
    private IntPtr extFileObject;
    private readonly Lock lockObject;
    private readonly ExtendedAudioFileWriterSettings settings;

    private protected ExtendedAudioFileServicesWriter(IntPtr hExtFileObject, ExtendedAudioFileWriterSettings settings)
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        length = 0L;
        disposed = false;
        lockObject = new();
        this.settings = settings;
        extFileObject = hExtFileObject;
    }

    // A record class describing the available formats for
    // a given file type that the writer is able to write.
    private record FormatInfo(AudioFileTypeID ID, params AudioStreamBasicDescription[] Descriptions) { }

    // Helper method to create an InvalidOperationException with a message that includes the list of writable MIME types on the system.
    private static InvalidOperationException CreateNoSuitableFormatException(ExtendedAudioFileWriterSettings settings)
    {
        string writableTypes;
        ImmutableArray<string> types = default;
        try
        {
            types = AudioFileLibraryInformation.SupportedWritableMimeTypes;
            writableTypes = string.Join(", ", types);
        }
        catch
        {
            // Don't let a failure to build the hint hide the real error.
            writableTypes = "(could not be retrieved)";
        }
        // Ensure that the exception data property 'WritableMimeTypes' always
        // gets a valid result. Enumerating directly on default ImmutableArray instances
        // throws NullReferenceException, because it's backing array is null.
        //
        // Do not put the empty immutable array assignment to the variable
        // declaration. While that avoids the below if check, it does not
        // guarantee to us that it's backing array will keep a valid
        // non-null value, because if the 'SupportedWritableMimeTypes'
        // property is misbehaving, it could return 'default' which again
        // would make the enumeration to fail.
        if (types.IsDefault) { types = ImmutableArray<string>.Empty; }

        var e = new InvalidOperationException(
            $"Could not find a suitable audio format to write MIME type '{settings.FileType}' " +
            $"from {settings.ProvidingFormat}. Writable MIME types on this system: {writableTypes}"
        );
        // Provide the supported writable MIME types and the selected MIME type
        // and format in the exception data, if the users want to inspect them.
        e.Data.Add("WritableMimeTypes", types);
        e.Data.Add("SelectedMimeType", settings.FileType);
        e.Data.Add("SelectedFormat", settings.ProvidingFormat);
        return e;
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
            return [
                AudioFormatIDs.kAudioFormatAC3,
                AudioFormatIDs.kAudioFormatEnhancedAC3,
                AudioFormatIDs.kAudioFormat60958AC3
            ];
        }
        else if (
            id == AudioFileTypeIDs.kAudioFileAAC_ADTSType ||
            id == AudioFileTypeIDs.kAudioFileLATMInLOASType // This type maps to the AAC files.
        )
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
                AudioFormatIDs.kAudioFormatMPEG4AAC_LD,
                AudioFormatIDs.kAudioFormatAppleLossless
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
                AudioFormatIDs.kAudioFormatAMR,
                AudioFormatIDs.kAudioFormatAMR_WB,
                AudioFormatIDs.kAudioFormatiLBC
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
            // We cannot find suitable audio formats for the provided ID,
            // or the native headers were updated and declare
            // new type ID's we are not aware of.
            return [];
        }
    }

    private static IEnumerable<FormatInfo> GetBestFormats(ExtendedAudioFileWriterSettings settings)
    {
        bool providedResults = false;
        AudioFileTypeAndFormatID ffid = new();
        AudioFileTypeID[] fileTypeIDs = [];
        try
        {
            fileTypeIDs = AudioFileLibraryInformation.GetFileTypeIDsForMimeType(settings.FileType);
        }
        // See the end of this method.
        catch { }
        foreach (var id in fileTypeIDs)
        {
            foreach (var fmtId in SelectBestIDForFileType(ffid.mFileType = id))
            {
                ffid.mFormatID = fmtId;
                AudioStreamBasicDescription[] descriptions;
                try
                {
                    descriptions = AudioFileLibraryInformation.GetAvailableStreamDescriptionsForFormat(ffid);
                }
                // Ignore the exception, move to the next format
                catch { descriptions = []; }
                // If there are descriptions for this file type, return them.
                if (descriptions.Length > 0)
                {
                    providedResults = true;
                    yield return new(
                        id,
                        descriptions
                    );
                }
            }
        }
        if (!providedResults)
        {
            throw CreateNoSuitableFormatException(settings);
        }
    }

    // Attempts to find the best matching file format to use
    // for writing the new file.
    // It does that by trying to match the format bit depth
    // and IEEE float output requirement, and if there is not
    // a format that can be matched, it gives back an approximate
    // format that is at least the required bit depth. If no such
    // format can be even then found, it gives up and returns false,
    // that it's meaning is that a format cannot be found.
    private static bool FindBestFormat(ExtendedAudioFileWriterSettings settings, out AudioStreamBasicDescription description, out AudioFileTypeID id)
    {
        id = default;
        description = default;
        uint requiredBitsPerSample = (uint)settings.ProvidingFormat.BitsPerSample;
        // A list of all the stream formats that are very approximate to
        // the target bit depth and IEEE mode. Will be used if and only
        // if there is not a format that matches the requirements imposed
        // by the user.
        List<FormatInfo> bestCloseFormats = new();
        foreach (var formatInfo in GetBestFormats(settings))
        {
            // Find a best format close to the number of bits per sample of the providing format.
            int I = 0;
            int closestMatchIndex = -1;
            uint closestBitsPerSample = 0U;
            foreach (var cAsbd in formatInfo.Descriptions)
            {
                // If we are lucky enough to find a format that has
                // a bit depth that matches and IEEE float usage
                // on output, we use that one.
                if (
                    cAsbd.mBitsPerChannel == requiredBitsPerSample &&
                    settings.EmitIEEEFloatSamples ==
                    cAsbd.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsFloat)
                )
                {
                    description = cAsbd;
                    id = formatInfo.ID;
                    return true;
                }
                else if (cAsbd.mBitsPerChannel == 0U)
                {
                    // It means that we can use whatever bit depth we want.
                    // Probably this is a compressed format.
                    description = cAsbd;
                    id = formatInfo.ID;
                    return true;
                }
                // Otherwise, try to find the maximum bit depth we can use.
                // We might lose some information from this approximation,
                // but we cannot do something better than that.
                else if (
                    requiredBitsPerSample > closestBitsPerSample &&
                    cAsbd.mBitsPerChannel > closestBitsPerSample
                )
                {
                    closestBitsPerSample = cAsbd.mBitsPerChannel;
                    closestMatchIndex = I;
                }
                I++;
            }
            // Best and closest match is added to the list of the
            // best closest formats, will be queried if we do not
            // have a matching format that we can make use of.
            if (closestMatchIndex > -1)
            {
                bestCloseFormats.Add(new(formatInfo.ID, formatInfo.Descriptions[closestMatchIndex]));
            }
        }
        if (description.mFormatID == 0U)
        {
            // We did not found a format whose bit 
            // depth and IEEE float usage do match.
            // If we have an approximation we will use that.
            foreach (var closeFormat in bestCloseFormats)
            {
                var desc = closeFormat.Descriptions[0];
                // Generally, prioritize that format that has greater or equal bit depth.
                if (desc.mBitsPerChannel >= requiredBitsPerSample)
                {
                    description = desc;
                    id = closeFormat.ID;
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    internal static AudioStreamBasicDescription BuildWriter(ExtendedAudioFileWriterSettings settings, out AudioChannelLayout layout, out AudioFileTypeID fileTypeID)
    {
        var providingFormat = settings.ProvidingFormat;
        CheckValidFormat(providingFormat);
        if (!FindBestFormat(settings, out var fileAsbd, out fileTypeID))
        {
            throw CreateNoSuitableFormatException(settings);
        }
        fileAsbd.mSampleRate = providingFormat.SampleRate;
        fileAsbd.mChannelsPerFrame = (uint)providingFormat.Channels;
        if (fileAsbd.mFormatID == AudioFormatIDs.kAudioFormatLinearPCM)
        {
            // This is not defined from the selected description,
            // (because it is derived from the channel count),
            // so we define this ourselves.
            // Without it, the writer panics.
            fileAsbd.mBytesPerFrame = fileAsbd.mBytesPerPacket = (
                (
                    fileAsbd.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved) ?
                    1U :
                    fileAsbd.mChannelsPerFrame
                ) * (fileAsbd.mBitsPerChannel / 8U)
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

    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification = "Internal helper method"
    )]
    private static void CheckValidFormat(WaveFormat? fmt)
    {
        // We leave the null check here as-is, if in the future 
        // there are code changes that could allow a null value
        // to be defined in the settings object.
        if (fmt is null) { throw new ArgumentException("ProvidingFormat property in settings object was not assigned to a valid WaveFormat instance.", "settings"); }
        if (fmt is WaveFormatExtensible extensible)
        {
            if (
                extensible.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_PCM &&
                extensible.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT
            )
            {
                throw new ArgumentException("Providing format must be IEEE floating-point or PCM");
            }
        }
        else if (fmt.Encoding != WaveFormatEncoding.Pcm &&
                fmt.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Providing format must be IEEE floating-point or PCM");
        }
    }

    private protected IntPtr GetExtAudioFileHandle() => extFileObject;

    /// <summary>
    /// Gets the currently defined settings object for this extended file writer instance.
    /// </summary>
    public ExtendedAudioFileWriterSettings Settings => settings;

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
        set => throw new NotSupportedException("Seeking is not supported on extended audio file writer instances");
    }

    /// <inheritdoc />
    public sealed override long Length => length;

    /// <inheritdoc />
    public sealed override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Reading is not supported on extended audio file writer instances");

    /// <inheritdoc />
    public sealed override int Read(Span<byte> buffer)
        => throw new NotSupportedException("Reading is not supported on extended audio file writer instances");

    /// <summary>Writes audio data to the current writer.</summary>
    /// <param name="buffer">The buffer of audio data to provide to the file writer.</param>
    public unsafe sealed override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        uint bufferLength = (uint)buffer.Length;
        var outFormat = settings.ProvidingFormat;
        uint numFramesToWrite = MacUtils.GetNumberOfPacketsFromBytesAndFormat(bufferLength, outFormat);
        if (numFramesToWrite == 0U && bufferLength > 0U)
        {
            // We have a value less than BlockAlign.
            // Throw to avoid such subtle issues.
            throw new ArgumentException("Buffer length cannot be less than the stream's block alignment.", nameof(buffer));
        }
        fixed (byte* bufferPointer = buffer)
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileWrite(
                    extFileObject,
                    numFramesToWrite,
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
    public sealed override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException("Seeking is not supported on extended audio file writer instances");

    /// <inheritdoc />
    public sealed override void SetLength(long value)
        => throw new NotSupportedException("Setting length is not supported on extended audio file writer instances");

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
        lockObject.Enter();
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
            lockObject.Exit();
        }
    }
}
