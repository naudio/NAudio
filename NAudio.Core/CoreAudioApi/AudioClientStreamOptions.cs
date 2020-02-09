namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Defines values that describe the characteristics of an audio stream.
    /// </summary>
    public enum AudioClientStreamOptions
    {
        /// <summary>
        /// No stream options.
        /// </summary>
        None = 0,
        /// <summary>
        /// The audio stream is a 'raw' stream that bypasses all signal processing except for endpoint specific, always-on processing in the APO, driver, and hardware.
        /// </summary>
        Raw = 0x1
    }
}