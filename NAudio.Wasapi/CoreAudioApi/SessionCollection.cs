using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Collection of sessions.
    /// </summary>
    public class SessionCollection : IDisposable
    {
        private IAudioSessionEnumerator audioSessionEnumerator;
        private IntPtr nativePointer;

        /// <summary>
        /// Creates a new SessionCollection — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal SessionCollection(IntPtr nativePointer)
        {
            this.nativePointer = nativePointer;
            audioSessionEnumerator = (IAudioSessionEnumerator)Marshal.GetObjectForIUnknown(nativePointer);
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
                audioSessionEnumerator = null;
            }
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
