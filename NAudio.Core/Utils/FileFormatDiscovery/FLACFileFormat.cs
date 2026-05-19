
using System;
using System.IO;

namespace NAudio.Utils.FileFormatDiscovery
{
    /// <summary>
    /// Free Lossless Audio Codec file format.
    /// </summary>
    public sealed class FLACFileFormat : AudioFileFormat
    {
        private FLACFileFormat() { }

        /// <summary>
        /// Gets the single and only instance of the <see cref="FLACFileFormat"/> class.
        /// </summary>
        public static readonly FLACFileFormat Instance = new();

        /// <inheritdoc />
        public override string MimeTypeName => "flac";

        /// <inheritdoc />
        public override unsafe bool IsFormat(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            byte[] header = new byte[4];
            stream.ReadExactly(header);
            fixed (byte* hdr = header)
            {
                return new string((sbyte*)hdr, 0, 4) == "fLaC";
            }
        }
    }
}
