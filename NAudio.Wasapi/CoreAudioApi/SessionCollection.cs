using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Collection of sessions.
    /// </summary>
    public class SessionCollection : IDisposable
    {
        private IAudioSessionEnumerator audioSessionEnumerator;

        /// <summary>
        /// Creates a new SessionCollection — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal SessionCollection(IntPtr nativePointer)
        {
            try
            {
                audioSessionEnumerator = ComActivation.WrapUnique<IAudioSessionEnumerator>(nativePointer);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
        }

        /// <summary>
        /// Returns session at index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public AudioSessionControl this[int index]
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioSessionEnumerator.GetSession(index, out var ptr));
                return new AudioSessionControl(ptr);
            }
        }

        /// <summary>
        /// Number of current sessions.
        /// </summary>
        public int Count
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioSessionEnumerator.GetCount(out var result));
                return result;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioSessionEnumerator != null)
            {
                if ((object)audioSessionEnumerator is ComObject co)
                {
                    co.FinalRelease();
                }
                audioSessionEnumerator = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
