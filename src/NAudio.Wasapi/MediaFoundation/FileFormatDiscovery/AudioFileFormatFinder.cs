using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MediaFoundation.FileFormatDiscovery;

/// <summary>
/// Audio file format finder. <br />
/// Finds out what file format a given data stream uses. <br />
/// File formats to test can be registered in two ways: <br />
/// <list type="bullet">
///     <item>Registering it as a default file format using the static <see cref="AddDefaultFileFormat(AudioFileFormat)"/> method.</item>
///     <item>Registering during buliding with the <see cref="AddFileFormatToTest(AudioFileFormat)"/> method.</item>
/// </list>
/// This class adopts and enforces the builder pattern.
/// </summary>
internal sealed class AudioFileFormatFinder
{
    private static readonly List<AudioFileFormat> defaultFileFormatsRegistry;

    static AudioFileFormatFinder()
    {
        defaultFileFormatsRegistry = new(3);
        defaultFileFormatsRegistry.Add(Mp3FileFormat.Instance);
        defaultFileFormatsRegistry.Add(Mp4FileFormat.Instance);
        defaultFileFormatsRegistry.Add(FlacFileFormat.Instance);
    }

    /// <summary>
    /// Adds a file format to be tested on all file format finder instances that call the <see cref="AddDefaultFileFormats"/> method. <br />
    /// Note, you do not need to call this for file formats provided by the Core library, this class adds them during static initialization.
    /// </summary>
    /// <param name="format">The default file format to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is <see langword="null"/>.</exception>
    public static void AddDefaultFileFormat(AudioFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        Monitor.Enter(defaultFileFormatsRegistry);
        try
        {
            defaultFileFormatsRegistry.Add(format);
        }
        finally
        {
            Monitor.Exit(defaultFileFormatsRegistry);
        }
    }

    private readonly List<AudioFileFormat> fileFormatsToTry;

    /// <summary>
    /// Initializes a new and empty instance of the <see cref="AudioFileFormatFinder"/> class.
    /// </summary>
    public AudioFileFormatFinder() => fileFormatsToTry = new(10);

    /// <summary>
    /// Adds to the current file format finder the default file formats that are added using the <see cref="AddDefaultFileFormat(AudioFileFormat)"/> method.
    /// </summary>
    /// <returns>The current instance.</returns>
    [return: NotNull]
    public AudioFileFormatFinder AddDefaultFileFormats()
    {
        Monitor.Enter(defaultFileFormatsRegistry);
        try
        {
            fileFormatsToTry.AddRange(defaultFileFormatsRegistry);
        }
        finally
        {
            Monitor.Exit(defaultFileFormatsRegistry);
        }
        return this;
    }

    /// <summary>
    /// Adds to the current file format finder the specified audio file format.
    /// </summary>
    /// <param name="format">The audio file format to add.</param>
    /// <returns>The current instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is <see langword="null"/>.</exception>
    [return: NotNull]
    public AudioFileFormatFinder AddFileFormatToTest(AudioFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        fileFormatsToTry.Add(format);
        return this;
    }

    /// <summary>
    /// Adds to the current file format finder the specified audio file format(s).
    /// </summary>
    /// <param name="formats">The audio file format(s) to add.</param>
    /// <returns>The current instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formats"/> is <see langword="null"/>.</exception>
    [return: NotNull]
    public AudioFileFormatFinder AddFileFormatsToTest(params AudioFileFormat[] formats)
    {
        ArgumentNullException.ThrowIfNull(formats);
        fileFormatsToTry.AddRange(formats);
        return this;
    }

    /// <summary>Removes all the currently added file formats.</summary>
    /// <returns>The current instance.</returns>
    [return: NotNull]
    public AudioFileFormatFinder ClearFileFormats()
    {
        fileFormatsToTry.Clear();
        return this;
    }

    /// <summary>
    /// Finds out which file format is the current data stream. <br />
    /// The data stream must be both readable and seekable.
    /// </summary>
    /// <param name="stream">The data stream to find it's audio file format.</param>
    /// <returns>
    /// The <see cref="AudioFileFormat"/> that is believed to be the current data stream. <br />
    /// Will return <see langword="null"/> if no suitable file format can be found.
    /// </returns>
    /// <remarks>
    /// Once an outcome has been determined, the method will automatically reposition 
    /// the stream back to where it was before it was provided to this method.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unreadable and/or unseekable.</exception>
    [return: MaybeNull]
    public AudioFileFormat FindFormat(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }
        else if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable.", nameof(stream));
        }
        else
        {
            long initial_position = stream.Position;
            try
            {
                foreach (AudioFileFormat fmt in fileFormatsToTry)
                {
                    stream.Position = initial_position;
                    try
                    {
                        // Do not care about the thrown exceptions from this, except if they are critical (such as OutOfMemoryException ones)
                        if (fmt.IsFormat(stream)) { return fmt; }
                    }
                    catch (AccessViolationException)
                    {
                        throw;
                    }
                    catch (StackOverflowException)
                    {
                        throw;
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
#if DEBUG
                    // COM (external) exceptions may be also critical.
                    // On Debug builds, throw them too to verify whether they are critical errors or not.
                    catch (System.Runtime.InteropServices.ExternalException) { throw; }
#endif
                    catch { }
                }
                return null;
            }
            finally
            {
                try { stream.Position = initial_position; } catch { }
            }
        }
    }

}
