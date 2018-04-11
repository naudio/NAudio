// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio SimpleAudioVolume
    /// </summary>
    public class SimpleAudioVolume : IDisposable
    {
        private readonly ISimpleAudioVolume simpleAudioVolume;

        /// <summary>
        /// Creates a new Audio endpoint volume
        /// </summary>
        /// <param name="realSimpleVolume">ISimpleAudioVolume COM interface</param>
        internal SimpleAudioVolume(ISimpleAudioVolume realSimpleVolume)
        {
            simpleAudioVolume = realSimpleVolume;
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~SimpleAudioVolume()
        {
            Dispose();
        }

        #endregion

        /// <summary>
        /// Allows the user to adjust the volume from
        /// 0.0 to 1.0
        /// </summary>
        public float Volume
        {
            get
            {
                Marshal.ThrowExceptionForHR(simpleAudioVolume.GetMasterVolume(out var result));
                return result;
            }
            set
            {
                if ((value >= 0.0) && (value <= 1.0))
                {
                    Marshal.ThrowExceptionForHR(simpleAudioVolume.SetMasterVolume(value, Guid.Empty));
                }
                // should throw something if out of range
            }
        }

        /// <summary>
        /// Mute
        /// </summary>
        public bool Mute
        {
            get
            {
                Marshal.ThrowExceptionForHR(simpleAudioVolume.GetMute(out var result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(simpleAudioVolume.SetMute(value, Guid.Empty));
            }
        }
    }
}
