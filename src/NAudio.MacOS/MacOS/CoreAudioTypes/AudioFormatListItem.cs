// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioFormatListItem <br />
/// this struct is used as output from the kAudioFormatProperty_FormatList property
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioFormatListItem
{
    /// <summary>
    /// an <see cref="AudioStreamBasicDescription"/>
    /// </summary>
    public AudioStreamBasicDescription mASBD;
    /// <summary>
    /// an <see cref="AudioChannelLayoutTag"/>
    /// </summary>
    public AudioChannelLayoutTag mChannelLayoutTag;
}