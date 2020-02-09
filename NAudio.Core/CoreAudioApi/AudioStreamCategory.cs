namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Specifies the category of an audio stream.
    /// </summary>
    public enum AudioStreamCategory
    {
        /// <summary>
        /// Other audio stream.
        /// </summary>
        Other = 0,
        /// <summary>
        /// Media that will only stream when the app is in the foreground.
        /// </summary>
        ForegroundOnlyMedia,
        /// <summary>
        /// Media that can be streamed when the app is in the background.
        /// </summary>
        BackgroundCapableMedia,
        /// <summary>
        /// Real-time communications, such as VOIP or chat.
        /// </summary>
        Communications,
        /// <summary>
        /// Alert sounds.
        /// </summary>
        Alerts,
        /// <summary>
        /// Sound effects.
        /// </summary>
        SoundEffects,
        /// <summary>
        /// Game sound effects.
        /// </summary>
        GameEffects,
        /// <summary>
        /// Background audio for games.
        /// </summary>
        GameMedia,
    }
}