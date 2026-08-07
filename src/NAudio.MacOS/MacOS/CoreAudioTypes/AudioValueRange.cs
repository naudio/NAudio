// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// This structure holds a pair of numbers that represent a continuous range of values.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioValueRange
{
    /// <summary>The minimum value.</summary>
    public readonly double mMinimum;
    /// <summary>The maximum value.</summary>
    public readonly double mMaximum;
}

