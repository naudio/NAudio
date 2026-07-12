/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

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