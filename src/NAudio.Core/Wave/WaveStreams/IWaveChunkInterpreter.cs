// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Converts one or more raw RIFF chunks into a strongly-typed view.
    /// Implementations are typically stateless; expose a <c>public static readonly</c> instance
    /// and call it via <see cref="WaveChunks.Read{T}(IWaveChunkInterpreter{T})"/>.
    /// </summary>
    /// <typeparam name="T">The result type. Returns <c>default</c> if the required chunks are absent.</typeparam>
    public interface IWaveChunkInterpreter<out T>
    {
        /// <summary>
        /// Reads any required chunks from <paramref name="chunks"/> and returns the interpreted result,
        /// or <c>default</c> if the chunks needed to construct a meaningful result are not present.
        /// </summary>
        T Interpret(WaveChunks chunks);
    }
}
