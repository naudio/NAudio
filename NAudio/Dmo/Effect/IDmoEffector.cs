using System;

namespace NAudio.Dmo.Effect
{
    /// <summary>
    /// Interface of DMO Effectors
    /// </summary>
    /// <typeparam name="TParameters">Parameters of the effect to be used</typeparam>
    public interface IDmoEffector<out TParameters> : IDisposable
    {
        /// <summary>
        /// Media Object
        /// </summary>
        MediaObject MediaObject { get; }

        /// <summary>
        /// Media Object InPlace
        /// </summary>
        MediaObjectInPlace MediaObjectInPlace { get; }

        /// <summary>
        /// Effect Parameter
        /// </summary>
        TParameters EffectParams { get; }
    }
}