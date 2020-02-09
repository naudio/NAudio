namespace NAudio.Utils
{
    /// <summary>
    /// Helper methods for working with audio buffers
    /// </summary>
    public static class BufferHelpers
    {
        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bytesRequired"></param>
        /// <returns></returns>
        public static byte[] Ensure(byte[] buffer, int bytesRequired)
        {
            if (buffer == null || buffer.Length < bytesRequired)
            {
                buffer = new byte[bytesRequired];
            }
            return buffer;
        }

        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="samplesRequired"></param>
        /// <returns></returns>
        public static float[] Ensure(float[] buffer, int samplesRequired)
        {
            if (buffer == null || buffer.Length < samplesRequired)
            {
                buffer = new float[samplesRequired];
            }
            return buffer;
        }
    }
}
