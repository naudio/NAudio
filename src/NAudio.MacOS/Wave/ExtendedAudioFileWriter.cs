
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
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.3.1")]
public unsafe partial class ExtendedAudioFileWriter : AbstractExtendedFileWriter
{
    private ExtendedAudioFileWriter(nint hExtFileObject, ExtendedFileWriterSettings settings)
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
        if (format is null) { throw new ArgumentException("OutputFormat property in settings object was not assigned to a valid WaveFormat instance.", "settings"); }

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
    public static ExtendedAudioFileWriter CreateFromURL(
        Uri url,
        ExtendedFileWriterSettings settings,
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
    public static ExtendedAudioFileWriter CreateFromFilePath(
        string filePath,
        ExtendedFileWriterSettings settings,
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

        ExtendedAudioFileException.ThrowIfError(
            NativeMethods.ExtAudioFileCreateWithURL(
                urlNative.DangerousGetObject(),
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
        ExtendedFileWriterSettings settings
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