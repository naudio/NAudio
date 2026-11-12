// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

using System.Runtime.InteropServices;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueueParameterEvent <br />
/// Specifies a value for an audio queue parameter.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioQueueParameterEvent
{
    /// <summary>The parameter.</summary>
    public AudioQueueParameterID mID;
    /// <summary>The value of the specified parameter.</summary>
    public float mValue;
}
