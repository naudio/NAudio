/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

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