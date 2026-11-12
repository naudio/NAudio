// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

using System.Runtime.InteropServices;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueueLevelMeterState <br />
/// Specifies the current level metering information for one channel of an audio queue.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioQueueLevelMeterState
{
    /// <summary>The audio channel's average RMS power.</summary>
    public float mAveragePower;
    /// <summary>The audio channel's peak RMS power.</summary>
    public float mPeakPower;
}
