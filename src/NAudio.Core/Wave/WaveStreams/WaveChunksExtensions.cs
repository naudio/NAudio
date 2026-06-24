// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Convenience extension methods over <see cref="WaveChunks"/> for NAudio's built-in
    /// chunk interpreters. Each extension simply invokes the corresponding
    /// <c>IWaveChunkInterpreter&lt;T&gt;.Instance</c> — users can write their own interpreters
    /// (or extension methods) following the same pattern.
    /// </summary>
    public static class WaveChunksExtensions
    {
        /// <summary>
        /// Reads the <c>cue</c> and companion <c>LIST/adtl</c> chunks, returning a <see cref="CueList"/>.
        /// Returns <c>null</c> if no cue list is present.
        /// </summary>
        public static CueList ReadCueList(this WaveChunks chunks)
            => chunks?.Read(CueListInterpreter.Instance);

        /// <summary>
        /// Reads the Broadcast Wave Format <c>bext</c> chunk, returning a <see cref="BroadcastExtension"/>.
        /// Returns <c>null</c> if no <c>bext</c> chunk is present.
        /// </summary>
        public static BroadcastExtension ReadBroadcastExtension(this WaveChunks chunks)
            => chunks?.Read(BextInterpreter.Instance);

        /// <summary>
        /// Reads the <c>LIST/INFO</c> metadata chunk, returning an <see cref="InfoMetadata"/>.
        /// Returns <c>null</c> if no INFO list is present.
        /// </summary>
        public static InfoMetadata ReadInfoMetadata(this WaveChunks chunks)
            => chunks?.Read(InfoListInterpreter.Instance);
    }
}
