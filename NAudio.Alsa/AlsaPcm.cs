using System;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// Shared lifetime for an open ALSA PCM device. Owns the
    /// <see cref="SafePcmHandle"/>; the audio data path is added by the
    /// playback / capture subclasses.
    /// </summary>
    internal abstract class AlsaPcm : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Opens the named PCM device for the given stream direction.
        /// </summary>
        /// <param name="pcmName">An ALSA PCM name, e.g. <c>"default"</c> or <c>"hw:0"</c>.</param>
        /// <param name="stream">Playback or capture.</param>
        protected AlsaPcm(string pcmName, PCMStream stream)
        {
            AlsaException.ThrowIfError(
                AlsaInterop.PcmOpen(out var handle, pcmName, stream, 0),
                "snd_pcm_open");
            Handle = new SafePcmHandle(handle);
        }

        /// <summary>The open PCM handle.</summary>
        protected SafePcmHandle Handle { get; }

        /// <summary>Throws if this instance has already been disposed.</summary>
        protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

        /// <summary>
        /// Releases native PCM resources held by the subclass. The base
        /// class closes <see cref="Handle"/> after this returns.
        /// </summary>
        protected virtual void DisposeCore()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            DisposeCore();
            Handle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
