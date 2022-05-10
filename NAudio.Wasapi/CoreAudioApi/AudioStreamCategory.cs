namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Specifies the category of an audio stream.
    /// https://docs.microsoft.com/en-us/windows/win32/api/audiosessiontypes/ne-audiosessiontypes-audio_stream_category
    /// AUDIO_STREAM_CATEGORY
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
        /// <summary>
        /// Game chat audio. Similar to AudioCategory_Communications except that AudioCategory_GameChat will not attenuate other streams.
        /// </summary>
        GameChat,
        /// <summary>
        /// Speech
        /// </summary>
        Speech,
        /// <summary>
        /// Stream that includes audio with dialog.
        /// </summary>
        Movie,
        /// <summary>
        /// Stream that includes audio without dialog.
        /// </summary>
        Media,
        /// <summary>
        /// Media is audio captured with the intent of capturing voice sources located in the ‘far field’. (Far away from the microphone.)
        /// </summary>
        FarFieldSpeech,
        /// <summary>
        /// Media is captured audio that requires consistent speech processing for the captured audio stream across all Windows devices. Used by applications that process speech data using machine learning algorithms.
        /// </summary>
        UniformSpeech,
        /// <summary>
        /// Media is audio captured with the intent of enabling dictation or typing by voice.
        /// </summary>
        VoiceTyping
    }
}