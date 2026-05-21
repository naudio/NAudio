
using System;
using System.IO;

namespace NAudio.Utils.FileFormatDiscovery
{
    /// <summary>
    /// MPEG 4 container file format.
    /// </summary>
    internal sealed class Mp4FileFormat : AudioFileFormat
    {
        private Mp4FileFormat() { }

        /// <summary>
        /// Gets the single and only instance of the <see cref="Mp4FileFormat"/> class.
        /// </summary>
        public static readonly Mp4FileFormat Instance = new();

        /// <inheritdoc />
        public override string MimeTypeName => "mp4";

        /// <inheritdoc />
        public override unsafe bool IsFormat(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            byte[] header = new byte[4];
            stream.ReadExactly(header); // Size of the header
            stream.ReadExactly(header); // Identifier type, must be ftyp
            fixed (byte* hdr = header)
            {
                return new string((sbyte*)hdr, 0, 4) == "ftyp";
            }
        }
    }
}
