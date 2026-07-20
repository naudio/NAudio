using System;
using System.IO;

namespace NAudio.MediaFoundation.FileFormatDiscovery;

/// <summary>
/// Ogg container file format (Vorbis/Opus/FLAC-in-Ogg). <br />
/// Every Ogg page begins with the four-byte "OggS" capture pattern, so the
/// container is trivially identifiable regardless of the codec inside it.
/// </summary>
internal sealed class OggFileFormat : AudioFileFormat
{
    private OggFileFormat() { }

    /// <summary>
    /// Gets the single and only instance of the <see cref="OggFileFormat"/> class.
    /// </summary>
    public static readonly OggFileFormat Instance = new();

    /// <inheritdoc />
    public override string MimeTypeName => "ogg";

    /// <inheritdoc />
    public override unsafe bool IsFormat(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        byte[] header = new byte[4];
        stream.ReadExactly(header);
        fixed (byte* hdrPointer = header)
        {
            return new string((sbyte*)hdrPointer, 0, 4) == "OggS";
        }
    }
}
