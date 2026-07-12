/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.MacOS.CoreAudioTypes;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioStreamRangedDescription <br />
/// This structure allows a specific sample rate range to be associated with an
/// AudioStreamBasicDescription that specifies its sample rate as
/// kAudioStreamAnyRate.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioStreamRangedDescription
{
    /// <summary>
    /// The AudioStreamBasicDescription that describes the format of the stream.
    /// Note that the mSampleRate field of the structure will be the same as the
    /// the values in mSampleRateRange when only a single sample rate is supported.
    /// It will be kAudioStreamAnyRate when there is a range with more elements. 
    /// </summary>
    public AudioStreamBasicDescription mFormat;
    /// <summary>
    /// The AudioValueRange that describes the minimum and maximum sample rate for
    /// the stream. If the mSampleRate field of mFormat is kAudioStreamAnyRate the
    /// format supports the range of sample rates described by this structure.
    /// Otherwise, the minimum will be the same as the maximum which will be the
    /// same as the mSampleRate field of mFormat.
    /// </summary>
    public AudioValueRange mSampleRateRange;
}