using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Collection of sessions.
    /// </summary>
    public class SessionCollection
    {
        readonly IAudioSessionEnumerator audioSessionEnumerator;

        internal SessionCollection(IAudioSessionEnumerator realEnumerator)
        {
            audioSessionEnumerator = realEnumerator;
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
                var result = (IAudioSessionControl)Marshal.GetObjectForIUnknown(ptr);
                Marshal.Release(ptr);
                return new AudioSessionControl(result);
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
    }
}
