
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

/// <summary>
/// Provides the ability to decode and read files from data streams. <br />
/// The reader does the best effort to decode to PCM - as such, all the audio
/// file data that you will recieve will not ever be compressed formats. <br />
/// Unlike the <see cref="ExtendedAudioFileReaderFromURL"/> class,
/// it requires to set up either a MIME type or a file extension to
/// be able to better recognize the data stream.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.3.1")]
public sealed class ExtendedAudioFileReaderFromStream : AbstractExtendedFileReader
{
    private GCHandle streamGcHandle;
    private IntPtr handleToAudioFile;

    /// <summary>
    /// Provides additional settings pertaining only 
    /// to <see cref="ExtendedAudioFileReaderFromStream"/>
    /// instances.
    /// </summary>
    public sealed class ExtendedAudioFileReaderFromStreamSettings : ExtendedFileReaderSettings
    {
        /// <summary>
        /// Gets/sets the MIME type of the provided data stream.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets/sets the name of the file that it's data are provided to 
        /// the stream that will be used to deduce the file type.
        /// </summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedAudioFileReaderFromStream"/>
    /// class with the specified source data stream and the specified settings object.
    /// </summary>
    /// <param name="stream">The data stream to read the audio data from.</param>
    /// <param name="settings">The settings object to use for configuring the audio file reader.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unreadable or unseekable. -or- The stream cannot be decoded.</exception>
    public ExtendedAudioFileReaderFromStream(Stream stream, [AllowNull] ExtendedAudioFileReaderFromStreamSettings settings = null)
        : base(settings)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream is unreadable.", nameof(stream));
        }
        else if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream is unseekable.", nameof(stream));
        }
        else
        {
            handleToAudioFile = IntPtr.Zero;
            streamGcHandle = GCHandle.Alloc(
                new AudioFileCallbacks.CallbacksUserData(stream),
                GCHandleType.Normal
            );
            Init();
        }
    }

    /// <inheritdoc />
    protected unsafe override nint InitializeReader()
    {
        // Try to query all the file type ID's for the specified
        // MIME type or file name.
        // MIME type gets precendence over the file name;
        // because the MIME type is a more formal description 
        // than the file name.
        AudioFileTypeID[] ids = [0];
        var sets = (ExtendedAudioFileReaderFromStreamSettings)Settings;
        Stream streamObject = ((AudioFileCallbacks.CallbacksUserData)streamGcHandle.Target).Stream;
        if (sets is not null)
        {
            if (!string.IsNullOrWhiteSpace(sets.MimeType))
            {
                ids = AudioFileLibraryInformation.GetFileTypeIDsForMimeType(sets.MimeType);
            }
            else
            {
                var cn = Path.GetExtension(sets.FileName);
                if (!string.IsNullOrEmpty(cn))
                {
                    // We only need the extension of the file name.
                    ids = AudioFileLibraryInformation.GetTypesForExtension(cn[1..]);
                }
            }
        }
        long initPosition = 0L;
        try { initPosition = streamObject.Position; } catch { }
        int osStatus;
        Exception exception = null;
        try
        {
            foreach (var id in ids)
            {
                // Open the file always in read-only mode
                // to avoid modifying the caller's stream
                // in unexpected ways.
                osStatus = NativeMethods.AudioFileOpenWithCallbacks(
                    GCHandle.ToIntPtr(streamGcHandle),
                    AudioFileCallbacks.ReadProcedure,
                    null,
                    AudioFileCallbacks.GetSizeProcedure,
                    null,
                    id,
                    out handleToAudioFile
                );
                if (osStatus == AudioFileErrors.kAudioFileInvalidFileError)
                {
                    // The reader did not manage to appropriately find the format now, 
                    // it may be the the case that it is a format closely resemblant
                    // to two standard audio formats - so move to the next type ID
                    // to see if the reader will finally manage to read the stream.
                    try { streamObject.Position = initPosition; } catch { }
                    continue;
                }
                else if (osStatus == AudioFileCallbacks.CallbacksUserData.CustomErrorConst)
                {
                    throw ((AudioFileCallbacks.CallbacksUserData)streamGcHandle.Target).Exception;
                }
                else
                {
                    AudioFileException.ThrowIfError(osStatus);
                }
                ExtendedAudioFileException.ThrowIfError(
                    NativeMethods.ExtAudioFileWrapAudioFileID(
                        handleToAudioFile,
                        MacOS.MacBoolean.False,
                        out var outExtAudioFile
                    )
                );
                return outExtAudioFile;
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        DisposeAudioFileAndGCHandle();
        throw new ArgumentException("Could not initialize stream for reading: Cannot find a suitable format to use.", exception);
    }

    private void DisposeAudioFileAndGCHandle()
    {
        try
        {
            if (handleToAudioFile != IntPtr.Zero)
            {
                AudioFileException.ThrowIfError(
                    NativeMethods.AudioFileClose(handleToAudioFile)
                );
                handleToAudioFile = IntPtr.Zero;
            }
        }
        finally
        {
            if (streamGcHandle.IsAllocated)
            {
                streamGcHandle.Free();
            }
        }
    }

    /// <inheritdoc />
    protected override void DisposeNativeData()
    {
        base.DisposeNativeData();
        DisposeAudioFileAndGCHandle();
    }
}