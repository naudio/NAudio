
using System;
using System.Runtime.Versioning;

using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

/// <summary>
/// Provides the ability to decode and read files from URL paths. <br />
/// The reader does the best effort to decode to PCM - as such, all the audio
/// file data that you will recieve will not ever be compressed formats.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.3.1")]
public sealed class ExtendedAudioFileReaderFromURL : ExtendedAudioFileServicesReader
{
    private readonly object url;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedAudioFileReaderFromURL"/>
    /// class, specifying the URL to use, as well as the settings to apply to this reader.
    /// </summary>
    /// <param name="URL">The URL to the audio file to initialize from.</param>
    /// <param name="settings">Behavioral settings to apply to the reader.</param>
    public ExtendedAudioFileReaderFromURL(Uri URL, ExtendedAudioFileReaderSettings settings = null) : this(URLObject: URL, settings) { }

    private ExtendedAudioFileReaderFromURL(object URLObject, ExtendedAudioFileReaderSettings settings = null)
        : base(settings)
    {
        ArgumentNullException.ThrowIfNull(URLObject);
        url = URLObject;
        Init();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedAudioFileReaderFromURL"/>
    /// class, specifying the file path to use, as well as the settings to apply to this reader.
    /// </summary>
    /// <param name="filePath">The file path to the audio file to initialize from.</param>
    /// <param name="settings">Behavioral settings to apply to the reader.</param>
    public static ExtendedAudioFileReaderFromURL CreateFromFile(string filePath, ExtendedAudioFileReaderSettings settings = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return new ExtendedAudioFileReaderFromURL(URLObject: filePath, settings);
    }

    /// <inheritdoc />
    protected override nint InitializeReader()
    {
        MacOS.CoreFoundationApi.CFURL urlNative;
        if (url is Uri uri)
        {
            urlNative = MacOS.CoreFoundationApi.CFURL.CreateFromUri(uri);
        }
        else if (url is string str)
        {
            urlNative = MacOS.CoreFoundationApi.CFURL.CreateFromFilePath(str, false);
        }
        else
        {
            throw new InvalidOperationException("Invalid code path reached; the URL could not be initialized.");
        }
        try
        {
            ExtendedAudioFileException.ThrowIfError(
                NativeMethods.ExtAudioFileOpenURL(
                    urlNative.NativeObject,
                    out var outExtAudioFile
                )
            );

            return outExtAudioFile;
        }
        finally
        {
            urlNative.Dispose();
        }
    }
}