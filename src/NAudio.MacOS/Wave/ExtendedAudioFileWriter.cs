
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

using NAudio.Utils;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

/// <summary>
/// Provides a writer that can create audio files
/// using the macOS Audio Toolbox framework.
/// </summary>
[SupportedOSPlatform("ios2.1")]
[SupportedOSPlatform("macos10.4")]
public unsafe partial class ExtendedAudioFileWriter : ExtendedAudioFileServicesWriter
{
    private ExtendedAudioFileWriter(nint hExtFileObject, ExtendedAudioFileWriterSettings settings)
        : base(hExtFileObject, settings) { }

    // Initializes the writer.
    private protected void Init() => AssignClientFormat(Settings.ProvidingFormat);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification = "Common helper method"
    )]
    private void AssignClientFormat(WaveFormat format)
    {
        var asbd = MacUtils.ConstructASBDFromWaveFormat(format);

        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileSetProperty(
                GetExtAudioFileHandle(),
                ExtendedAudioFileProperties.kExtAudioFileProperty_ClientDataFormat,
                (uint)sizeof(AudioStreamBasicDescription),
                new(&asbd)
            )
        );

        if (format is WaveFormatExtensible ext && ext.ChannelMask != 0)
        {
            AudioChannelLayout l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileSetProperty(
                    GetExtAudioFileHandle(),
                    ExtendedAudioFileProperties.kExtAudioFileProperty_ClientChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    new(&l)
                )
            );
        }
    }

    // Removes the output of a failed creation attempt: either a file this call
    // created, or one it truncated through EraseFile. A file it never touched is
    // left alone. Cleanup problems are swallowed wholesale, deliberately - the
    // caller needs to see why the write failed, not why the tidying up did.
    private static void DeleteFailedOutput(string filePath, bool removeOnFailure)
    {
        if (!removeOnFailure) { return; }
        try
        {
            if (System.IO.File.Exists(filePath)) { System.IO.File.Delete(filePath); }
        }
        catch
        {
            // Nothing useful to do here, and throwing would hide the real error.
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="ExtendedAudioFileWriter" />
    /// class, providing the URL where the new produced file will be located to.
    /// </summary>
    /// <param name="url">The URL of the file to write</param>
    /// <param name="settings">Settings for the writer to use.</param>
    /// <param name="overwriteIfExists">If the file is existing, when specified to <see langword="true"/>, it overwrites the file instead of throwing an exception.</param>
    /// <returns>A new instance of the <see cref="ExtendedAudioFileWriter" /> class.</returns>
    /// <exception cref="ArgumentException">The specified settings object does not define the minimal requirements for writing a file.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> and/or <paramref name="settings"/> are <see langword="null"/>.</exception>
    [SupportedOSPlatform("macos10.5")]
    public static ExtendedAudioFileWriter CreateFromURL(
        Uri url,
        ExtendedAudioFileWriterSettings settings,
        bool overwriteIfExists = false
    )
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.FileType))
        {
            throw new ArgumentException("FileType is not assigned to a valid MIME type string", nameof(settings));
        }

        var asbd = BuildWriter(settings, out var layout, out var ft);

        using MacOS.CoreFoundationApi.CFURL urlNative = MacOS.CoreFoundationApi.CFURL.CreateFromUri(url);

        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileCreateWithURL(
                urlNative.NativeObject,
                ft,
                asbd,
                layout.mChannelLayoutTag == 0 ? IntPtr.Zero : new(&layout),
                overwriteIfExists ? AudioFileFlags.EraseFile : AudioFileFlags.None,
                out var outExtAudioFile
            )
        );

        try
        {
            var writer = new ExtendedAudioFileWriter(outExtAudioFile, settings);
            writer.Init();
            return writer;
        }
        catch
        {
            _ = NativeMethods.ExtAudioFileDispose(outExtAudioFile);
            throw;
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="ExtendedAudioFileWriter" />
    /// class, providing the file path where the new produced file will be located to.
    /// </summary>
    /// <param name="filePath">The path of the file to write</param>
    /// <param name="settings">Settings for the writer to use.</param>
    /// <param name="overwriteIfExists">If the file is existing, when specified to <see langword="true"/>, it overwrites the file instead of throwing an exception.</param>
    /// <returns>A new instance of the <see cref="ExtendedAudioFileWriter" /> class.</returns>
    /// <exception cref="ArgumentException">The specified settings object does not define the minimal requirements for writing a file.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> and/or <paramref name="settings"/> are <see langword="null"/>.</exception>
    [SupportedOSPlatform("macos10.5")]
    public static ExtendedAudioFileWriter CreateFromFilePath(
        string filePath,
        ExtendedAudioFileWriterSettings settings,
        bool overwriteIfExists = false
    )
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);

        if (string.IsNullOrWhiteSpace(settings.FileType))
        {
            throw new ArgumentException("FileType is not assigned to a valid MIME type string", nameof(settings));
        }

        var asbd = BuildWriter(settings, out var layout, out var ft);

        using MacOS.CoreFoundationApi.CFURL urlNative = MacOS.CoreFoundationApi.CFURL.CreateFromFilePath(filePath, false);

        // The native call creates the container before it validates the data
        // format against it, so a rejected combination (AAC cannot carry 32
        // channels, for instance) leaves a stub file behind that a later reader
        // then fails on with "the file is malformed".
        //
        // Cleanup therefore applies in two cases: a file this call brought into
        // existence, and an existing file that EraseFile has already truncated,
        // whose original contents are gone regardless. A file left untouched
        // because overwriteIfExists was false is never removed.
        bool removeOnFailure = overwriteIfExists || !System.IO.File.Exists(filePath);

        IntPtr outExtAudioFile;
        try
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileCreateWithURL(
                    urlNative.DangerousGetObject(),
                    ft,
                    asbd,
                    layout.mChannelLayoutTag == 0 ? IntPtr.Zero : new(&layout),
                    overwriteIfExists ? AudioFileFlags.EraseFile : AudioFileFlags.None,
                    out outExtAudioFile
                )
            );
        }
        catch
        {
            DeleteFailedOutput(filePath, removeOnFailure);
            throw;
        }

        try
        {
            var writer = new ExtendedAudioFileWriter(outExtAudioFile, settings);
            writer.Init();
            return writer;
        }
        catch
        {
            _ = NativeMethods.ExtAudioFileDispose(outExtAudioFile);
            DeleteFailedOutput(filePath, removeOnFailure);
            throw;
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="ExtendedAudioFileWriter" />
    /// class, providing the data stream where the encoded data will be placed to.
    /// </summary>
    /// <param name="writeableStream">The data stream where to write data to</param>
    /// <param name="settings">Settings for the writer to use.</param>
    /// <returns>A new instance of the <see cref="ExtendedAudioFileWriter" /> class.</returns>
    /// <exception cref="ArgumentException">
    /// The specified settings object does not define the minimal requirements for writing a file. <br /> <br />
    /// 
    /// -or- <br /> <br />
    /// 
    /// The stream is not writeable, seekable and readable.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="writeableStream"/> and/or <paramref name="settings"/> are <see langword="null"/>.</exception>
    /// <remarks>
    /// The native API requires that all the basic <see cref="Stream"/> implementations
    /// are functional - that is, the stream must be able to be read, written and sought.
    /// If you want to write to a stream that is not that capable, use an intermediate 
    /// <see cref="MemoryStream"/> instance to make sure the writer can work with it,
    /// then copy the data written to it to your target stream.
    /// </remarks>
    public static ExtendedAudioFileWriter CreateFromStream(
        Stream writeableStream,
        ExtendedAudioFileWriterSettings settings
    )
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(writeableStream);

        if (!writeableStream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(writeableStream));
        }
        if (!writeableStream.CanWrite)
        {
            throw new ArgumentException("Stream must be writeable.", nameof(writeableStream));
        }
        else if (!writeableStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable.", nameof(writeableStream));
        }
        else if (string.IsNullOrWhiteSpace(settings.FileType))
        {
            throw new ArgumentException("FileType is not assigned to a valid MIME type string", nameof(settings));
        }

        var asbd = BuildWriter(settings, out var layout, out var ft);

        GCHandle handle = GCHandle.Alloc(new AudioFileCallbacks.CallbacksUserData(writeableStream), GCHandleType.Normal);

        IntPtr outAudioFile = IntPtr.Zero, outExtAudioFile = IntPtr.Zero;
        try
        {
            int status = NativeMethods.AudioFileInitializeWithCallbacks(
                GCHandle.ToIntPtr(handle),
                AudioFileCallbacks.ReadProcedure,
                AudioFileCallbacks.WriteProcedure,
                AudioFileCallbacks.GetSizeProcedure,
                AudioFileCallbacks.SetSizeProcedure,
                ft,
                asbd,
                AudioFileFlags.None,
                out outAudioFile
            );
            if (status == AudioFileCallbacks.CallbacksUserData.CustomErrorConst)
            {
                throw ((AudioFileCallbacks.CallbacksUserData)handle.Target).Exception;
            }
            AudioFileException.ThrowIfError(status);

            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileWrapAudioFileID(
                    outAudioFile,
                    MacOS.MacBoolean.True,
                    out outExtAudioFile
                )
            );

            if (layout.mChannelLayoutTag != 0)
            {
                ExtendedAudioFileException.ThrowIfError(
                    NativeMethods.ExtAudioFileSetProperty(
                        outExtAudioFile,
                        ExtendedAudioFileProperties.kExtAudioFileProperty_FileChannelLayout,
                        (uint)sizeof(AudioChannelLayout),
                        new(&layout)
                    )
                );
            }
        }
        catch
        {
            if (outExtAudioFile != IntPtr.Zero)
            {
                _ = NativeMethods.ExtAudioFileDispose(outExtAudioFile);
            }
            if (outAudioFile != IntPtr.Zero)
            {
                _ = NativeMethods.AudioFileClose(outAudioFile);
            }
            if (handle.IsAllocated) { handle.Free(); }
            throw;
        }

        StreamImpl writer = null;
        try
        {
            writer = new StreamImpl(
                outExtAudioFile,
                outAudioFile,
                handle,
                settings
            );
            writer.Init();
            return writer;
        }
        catch
        {
            writer?.Dispose();
            throw;
        }
    }
}