using System;
using NAudio.Dmo;
using NAudio.Dmo.Effect;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Provide WaveProvider that can apply effects in real time using DMO.
    /// 
    /// If the audio thread is running on the STA thread, please generate and operate from the same thread.
    /// If the audio thread is running on the MTA thread, please operate on any MTA thread.
    /// </summary>
    /// <typeparam name="TDmoEffector">Types of DMO effectors to use</typeparam>
    /// <typeparam name="TEffectorParam">Parameters of the effect to be used</typeparam>
    public class DmoEffectWaveProvider<TDmoEffector, TEffectorParam> : IWaveProvider, IDisposable
        where TDmoEffector : IDmoEffector<TEffectorParam>, new()
    {
        private readonly IWaveProvider inputProvider;
        private readonly IDmoEffector<TEffectorParam> effector;

        /// <summary>
        /// Create a new DmoEffectWaveProvider
        /// </summary>
        /// <param name="inputProvider">Input Stream</param>
        public DmoEffectWaveProvider(IWaveProvider inputProvider)
        {
            this.inputProvider = inputProvider;
            effector = new TDmoEffector();

            var mediaObject = effector.MediaObject;

            if (mediaObject == null)
            {
                throw new NotSupportedException(@"Dmo Effector Not Supported: " + nameof(TDmoEffector));
            }

            if (!mediaObject.SupportsInputWaveFormat(0, inputProvider.WaveFormat))
            {
                throw new ArgumentException(@"Unsupported Input Stream format", nameof(inputProvider));
            }

            mediaObject.AllocateStreamingResources();
            mediaObject.SetInputWaveFormat(0, this.inputProvider.WaveFormat);
            mediaObject.SetOutputWaveFormat(0, this.inputProvider.WaveFormat);
        }

        /// <summary>
        /// Stream Wave Format
        /// </summary>
        public WaveFormat WaveFormat => inputProvider.WaveFormat;

        /// <summary>
        /// Reads data from input stream
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="offset">offset into buffer</param>
        /// <param name="count">Bytes required</param>
        /// <returns>Number of bytes read</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            var readNum = inputProvider.Read(buffer, offset, count);

            if (effector == null)
            {
                return readNum;
            }

            if (effector.MediaObjectInPlace.Process(readNum, offset, buffer, 0, DmoInPlaceProcessFlags.Normal)
                == DmoInPlaceProcessReturn.HasEffectTail)
            {
                var effectTail = new byte[readNum];
                while (effector.MediaObjectInPlace.Process(readNum, 0, effectTail, 0, DmoInPlaceProcessFlags.Zero) ==
                       DmoInPlaceProcessReturn.HasEffectTail)
                {
                }
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
            if (effector != null)
            {
                effector.MediaObject.FreeStreamingResources();
                effector.Dispose();
            }
        }
    }
}