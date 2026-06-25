
using System.IO;
using NAudio.Wave;

namespace NAudio.MediaFoundation.FileFormatDiscovery;

/// <summary>
/// MPEG Layer 3 file format.
/// </summary>
internal sealed class Mp3FileFormat : AudioFileFormat
{
    private Mp3FileFormat() { }

    /// <summary>
    /// Gets the single and only instance of the <see cref="Mp3FileFormat"/> class.
    /// </summary>
    public static readonly Mp3FileFormat Instance = new();

    /// <inheritdoc />
    public override string MimeTypeName => "mp3";

    /// <inheritdoc />
    public override bool IsFormat(Stream stream) => Mp3Frame.LoadFromStream(stream, false) is not null;
}
