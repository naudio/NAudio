
using System;
using System.IO;

namespace NAudio.Utils.FileFormatDiscovery
{
    /// <summary>
    /// Free Lossless Audio Codec file format.
    /// </summary>
    internal sealed class FlacFileFormat : AudioFileFormat
    {
        private FlacFileFormat() { }

        /// <summary>
        /// Gets the single and only instance of the <see cref="FlacFileFormat"/> class.
        /// </summary>
        public static readonly FlacFileFormat Instance = new();

        /// <inheritdoc />
        public override string MimeTypeName => "flac";

        /// <inheritdoc />
        public override unsafe bool IsFormat(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            byte[] header = new byte[4];
            stream.ReadExactly(header);
            fixed (byte* hdrPointer = header)
            {
                return new string((sbyte*)hdrPointer, 0, 4) == "fLaC";
            }
        }
    }
}
