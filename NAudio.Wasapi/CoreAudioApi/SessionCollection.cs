using NAudio.CoreAudioApi.Interfaces;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Collection of sessions.
    /// </summary>
    public class SessionCollection : IEnumerable<AudioSessionControl>
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
                Marshal.ThrowExceptionForHR(audioSessionEnumerator.GetSession(index, out var result));
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
                Marshal.ThrowExceptionForHR(audioSessionEnumerator.GetCount(out var result));
                return result;
            }
        }

        #region IEnumerable<AudioSessionControl> Members

        /// <summary>
        /// Get Enumerator
        /// </summary>
        /// <returns>AudioSessionControl enumerator</returns>
        public IEnumerator<AudioSessionControl> GetEnumerator()
        {
            for (int index = 0; index < Count; index++)
            {
                yield return this[index];
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
