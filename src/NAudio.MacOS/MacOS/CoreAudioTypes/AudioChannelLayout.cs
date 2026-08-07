// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelLayout <br />
/// This structure is used to specify channel layouts in files and hardware.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioChannelLayout
{
    /// <summary>
    /// The <see cref="AudioChannelLayoutTag"/> that indicates the layout.
    /// </summary>
    public AudioChannelLayoutTag mChannelLayoutTag;
    /// <summary>
    /// If <see cref="mChannelLayoutTag"/> is set to 
    /// <see cref="AudioChannelLayoutTag.kAudioChannelLayoutTag_UseChannelBitmap"/>, 
    /// this field is the channel usage bitmap.
    /// </summary>
    public AudioChannelBitmap mChannelBitmap;
    /// <summary>
    /// The number of items in the <see cref="mChannelDescriptions"/> array.
    /// </summary>
    public UInt32 mNumberChannelDescriptions;
    /// <summary>
    /// A variable length array of AudioChannelDescriptions that describe the layout.
    /// </summary>
    public AudioChannelDescription mChannelDescriptions; // this is a variable length array of mNumberChannelDescriptions elements

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AudioChannelDescription GetChannelDescription(IntPtr layout, uint descriptionIndex)
        => (&((AudioChannelLayout*)layout)->mChannelDescriptions)[descriptionIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetChannelDescription(IntPtr layout, uint descriptionIndex, AudioChannelDescription desc)
        => (&((AudioChannelLayout*)layout)->mChannelDescriptions)[descriptionIndex] = desc;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AudioChannelLayoutTag GetAudioChannelLayoutTag(IntPtr layout)
        => ((AudioChannelLayout*)layout)->mChannelLayoutTag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AudioChannelBitmap GetAudioChannelBitmap(IntPtr layout)
        => ((AudioChannelLayout*)layout)->mChannelBitmap;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetNumberOfChannelDescriptions(IntPtr layout)
        => ((AudioChannelLayout*)layout)->mNumberChannelDescriptions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetNumberOfChannelDescriptions(IntPtr layout, uint nChannels)
        => ((AudioChannelLayout*)layout)->mNumberChannelDescriptions = nChannels;
}