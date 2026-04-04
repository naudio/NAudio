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
        private ISimpleAudioVolume simpleAudioVolume;
        private IntPtr nativePointer;

        /// <summary>
        /// Creates a new SimpleAudioVolume wrapper — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal SimpleAudioVolume(IntPtr nativePointer)
        {
            this.nativePointer = nativePointer;
            simpleAudioVolume = (ISimpleAudioVolume)Marshal.GetObjectForIUnknown(nativePointer);
        }

        /// <summary>
        /// Creates a new SimpleAudioVolume wrapper from a borrowed interface (e.g. QI from AudioSessionControl).
        /// This instance does not own the COM pointer.
        /// </summary>
        /// <param name="borrowed">ISimpleAudioVolume obtained via QI on an existing RCW</param>
        internal SimpleAudioVolume(ISimpleAudioVolume borrowed)
        {
            simpleAudioVolume = borrowed;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (simpleAudioVolume != null)
            {
                simpleAudioVolume = null;
            }
            // Deterministic release when we own the native pointer.
            // When obtained via QI from AudioSessionControl, nativePointer is IntPtr.Zero
            // and the parent object manages the COM lifetime.
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allows the user to adjust the volume from
        /// 0.0 to 1.0
        /// </summary>
        public float Volume
        {
            get
            {
                CoreAudioException.ThrowIfFailed(simpleAudioVolume.GetMasterVolume(out var result));
                return result;
            }
            set
            {
                if ((value >= 0.0) && (value <= 1.0))
                {
                    CoreAudioException.ThrowIfFailed(simpleAudioVolume.SetMasterVolume(value, Guid.Empty));
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
                CoreAudioException.ThrowIfFailed(simpleAudioVolume.GetMute(out var result));
                return result;
            }
            set
            {
                CoreAudioException.ThrowIfFailed(simpleAudioVolume.SetMute(value, Guid.Empty));
            }
        }
    }
}
