using System;
using NAudio.Dmo;
using NAudio.Dmo.Effect;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Applies DMO effects to an audio source in real time.
    ///
    /// If the audio thread is running on the STA thread, please generate and operate from the same thread.
    /// If the audio thread is running on the MTA thread, please operate on any MTA thread.
    /// </summary>
    /// <typeparam name="TDmoEffector">Types of DMO effectors to use</typeparam>
    /// <typeparam name="TEffectorParam">Parameters of the effect to be used</typeparam>
    public class DmoEffectWaveProvider<TDmoEffector, TEffectorParam> : IAudioSource, IDisposable
        where TDmoEffector : IDmoEffector<TEffectorParam>, new()
    {
        private readonly IAudioSource inputSource;
        private readonly IDmoEffector<TEffectorParam> effector;

        /// <summary>
        /// Create a new DmoEffectWaveProvider
        /// </summary>
        /// <param name="inputSource">Input audio source</param>
        public DmoEffectWaveProvider(IAudioSource inputSource)
        {
            this.inputSource = inputSource;
            effector = new TDmoEffector();

            var mediaObject = effector.MediaObject;

            if (mediaObject == null)
            {
                throw new NotSupportedException(@"Dmo Effector Not Supported: " + nameof(TDmoEffector));
            }

            if (!mediaObject.SupportsInputWaveFormat(0, inputSource.WaveFormat))
            {
                throw new ArgumentException(@"Unsupported Input Stream format", nameof(inputSource));
            }

            mediaObject.AllocateStreamingResources();
            mediaObject.SetInputWaveFormat(0, this.inputSource.WaveFormat);
            mediaObject.SetOutputWaveFormat(0, this.inputSource.WaveFormat);
        }

        /// <summary>
        /// Stream Wave Format
        /// </summary>
        public WaveFormat WaveFormat => inputSource.WaveFormat;

        /// <summary>
        /// Reads data from input source with in-place DMO effect processing.
        /// Pins the span and passes it directly to the DMO, avoiding intermediate copies.
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            var readNum = inputSource.Read(buffer);
            if (effector != null && readNum > 0)
            {
                effector.MediaObjectInPlace.Process(buffer.Slice(0, readNum), 0, DmoInPlaceProcessFlags.Normal);
            }
            return readNum;
        }

        /// <summary>
        /// Get Effector Parameters
        /// </summary>
        public TEffectorParam EffectParams => effector.EffectParams;

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (effector != null)
            {
                effector.MediaObject.FreeStreamingResources();
                effector.Dispose();
            }
        }
    }
}
