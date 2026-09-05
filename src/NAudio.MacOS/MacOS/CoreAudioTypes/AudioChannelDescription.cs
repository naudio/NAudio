// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelDescription <br />
/// This structure describes a single channel.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AudioChannelDescription
{
    /// <summary>
    /// The <see cref="AudioChannelLabel"/> that describes the channel.
    /// </summary>
    public AudioChannelLabel mChannelLabel;
    /// <summary>
    /// Flags that control the interpretation of <see cref="mCoordinates"/>.
    /// </summary>
    public AudioChannelFlags mChannelFlags;
    /// <summary>
    /// An ordered triple that specifies a precise speaker location.
    /// </summary>
    public fixed float mCoordinates[3];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetCoordinateByIndex(AudioChannelCoordinateIndex index) => mCoordinates[(uint)index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCoordinateByIndex(AudioChannelCoordinateIndex index, float value) => mCoordinates[(uint)index] = value;
}