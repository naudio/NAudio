using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveOutUtils
    /// </summary>
    public static class WaveOutUtils
    {
        /// <summary>
        /// Get WaveOut Volume
        /// </summary>
        public static float GetWaveOutVolume(IntPtr hWaveOut, object lockObject)
        {
            int stereoVolume;
            MmResult result;
            lock (lockObject)
            {
                result = WaveInterop.waveOutGetVolume(hWaveOut, out stereoVolume);
            }
            MmException.Try(result, "waveOutGetVolume");
            return (stereoVolume & 0xFFFF) / (float)0xFFFF;
        }

        /// <summary>
        /// Set WaveOut Volume
        /// </summary>
        public static void SetWaveOutVolume(float value, IntPtr hWaveOut, object lockObject)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            if (value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            float left = value;
            float right = value;

            int stereoVolume = (int)(left * 0xFFFF) + ((int)(right * 0xFFFF) << 16);
            MmResult result;
            lock (lockObject)
            {
                result = WaveInterop.waveOutSetVolume(hWaveOut, stereoVolume);
            }
            MmException.Try(result, "waveOutSetVolume");
        }

        /// <summary>
        /// Get position in bytes
        /// </summary>
        public static long GetPositionBytes(IntPtr hWaveOut, object lockObject)
        {
            lock (lockObject)
            {
                var mmTime = new MmTime();
                mmTime.wType = MmTime.TIME_BYTES; // request results in bytes, TODO: perhaps make this a little more flexible and support the other types?
                MmException.Try(WaveInterop.waveOutGetPosition(hWaveOut, ref mmTime, Marshal.SizeOf(mmTime)), "waveOutGetPosition");

                if (mmTime.wType != MmTime.TIME_BYTES)
                    throw new Exception(string.Format("waveOutGetPosition: wType -> Expected {0}, Received {1}", MmTime.TIME_BYTES, mmTime.wType));

                return mmTime.cb;
            }
        }
    }
}
