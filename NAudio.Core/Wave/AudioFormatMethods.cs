

namespace NAudio.Wave
{
    /// <summary>
    /// Provides extension and factory methods for manipulating and creating <see cref="IAudioFormat"/> instances. <br />
    /// Experimental - will likely change in the future
    /// </summary>
    public static class AudioFormatMethods
    {
        /// <summary>
        /// Gets the size of a wave buffer equivalent to the latency in milliseconds.
        /// </summary>
        /// <param name="format">Input audio format (Injected by C# extension feature)</param>
        /// <param name="milliseconds">The milliseconds.</param>
        /// <returns></returns>
        public static int ConvertLatencyToByteSize(this IAudioFormat format, int milliseconds)
        {
            int bytes = (int)((format.AverageBytesPerSecond / 1000.0) * milliseconds);
            int blk_align = format.BlockAlign;
            int remainder = bytes % blk_align;
            if (remainder != 0)
            {
                // Return the upper BlockAligned
                bytes = bytes + blk_align - remainder;
            }
            return bytes;
        }
    }
}
