
using System.IO;
using NAudio.Wave;

namespace NAudio.Utils.FileFormatDiscovery
{
    /// <summary>
    /// MPEG Layer 3 file format.
    /// </summary>
    public sealed class MP3FileFormat : AudioFileFormat
    {
        private MP3FileFormat() { }

        /// <summary>
        /// Gets the single and only instance of the <see cref="MP3FileFormat"/> class.
        /// </summary>
        public static readonly MP3FileFormat Instance = new();

        /// <inheritdoc />
        public override string MimeTypeName => "mp3";

        /// <inheritdoc />
        public override bool IsFormat(Stream stream) => Mp3Frame.LoadFromStream(stream, false) is not null;
    }
}
