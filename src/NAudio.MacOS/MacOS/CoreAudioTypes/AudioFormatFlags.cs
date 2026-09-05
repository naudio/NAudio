// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using System;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// Standard <see cref="AudioFormatFlags"/> Values for <see cref="AudioStreamBasicDescription"/>
/// These are the standard <see cref="AudioFormatFlags"/> for use in the <see cref="AudioStreamBasicDescription.mFormatFlags"/> field of the <see cref="AudioStreamBasicDescription"/> structure.
/// </summary>
/// <remarks>
/// Typically, when an ASBD is being used, the fields describe the complete layout
/// of the sample data in the buffers that are represented by this description -
/// where typically those buffers are represented by an <see cref="AudioBuffer"/> that is
/// contained in an <see cref="AudioBufferList"/>. <br /> <br />
/// 
/// However, when an ASBD has the <see cref="kAudioFormatFlagIsNonInterleaved"/> flag, the
/// <see cref="AudioBufferList"/> has a different structure and semantic. In this case, the ASBD
/// fields will describe the format of ONE of the AudioBuffers that are contained in
/// the list, AND each AudioBuffer in the list is determined to have a single (mono)
/// channel of audio data. 
/// Then, the ASBD's <see cref="AudioStreamBasicDescription.mChannelsPerFrame"/> will indicate the
/// total number of <see cref="AudioBuffer"/>s that are contained within the <see cref="AudioBufferList"/> -
/// where each buffer contains one channel. This is used primarily with the
/// AudioUnit (and AudioConverter) representation of this list - and won't be found
/// in the AudioHardware usage of this structure.
/// </remarks>
[Flags]
internal enum AudioFormatFlags : uint
{
    /// <summary>Set for floating point, clear for integer.</summary>
    kAudioFormatFlagIsFloat                     = (1U << 0),     // 0x1
    /// <summary>Set for big endian, clear for little endian.</summary>
    kAudioFormatFlagIsBigEndian                 = (1U << 1),     // 0x2
    /// <summary>
    /// Set for signed integer, clear for unsigned integer. 
    /// This is only valid if kAudioFormatFlagIsFloat is clear.
    /// </summary>
    kAudioFormatFlagIsSignedInteger             = (1U << 2),     // 0x4
    /// <summary>
    /// Set if the sample bits occupy the entire available bits for the channel,
    /// clear if they are high or low aligned within the channel. Note that even if
    /// this flag is clear, it is implied that this flag is set if the
    /// AudioStreamBasicDescription is filled out such that the fields have the
    /// following relationship:
    /// <c>((mBitsPerSample / 8) * mChannelsPerFrame) == mBytesPerFrame</c>
    /// </summary>
    kAudioFormatFlagIsPacked                    = (1U << 3),     // 0x8
    /// <summary>
    /// Set if the sample bits are placed into the high bits of the channel, 
    /// clear for low bit placement. 
    /// This is only valid if kAudioFormatFlagIsPacked is clear.
    /// </summary>
    kAudioFormatFlagIsAlignedHigh               = (1U << 4),     // 0x10
    /// <summary>
    /// Set if the samples for each channel are located contiguously and the
    /// channels are layed out end to end, clear if the samples for each frame are
    /// layed out contiguously and the frames layed out end to end.
    /// </summary>
    kAudioFormatFlagIsNonInterleaved            = (1U << 5),     // 0x20
    /// <summary>
    /// Set to indicate when a format is non-mixable. Note that this flag is only
    /// used when interacting with the HAL's stream format information. It is not a
    /// valid flag for any other uses.
    /// </summary>
    kAudioFormatFlagIsNonMixable                = (1U << 6),     // 0x40
    /// <summary>
    /// Set if all the flags would be clear in 
    /// order to preserve 0 as the wild card value.
    /// </summary>
    kAudioFormatFlagsAreAllClear                = 0x80000000,

    /// <summary>
    /// The linear PCM flags contain a 6-bit bitfield indicating that an integer
    /// format is to be interpreted as fixed point. The value indicates the number
    /// of bits are used to represent the fractional portion of each sample value.
    /// This constant indicates the bit position (counting from the right) of the
    /// bitfield in <see cref="AudioStreamBasicDescription.mFormatFlags"/>.
    /// </summary>
    kLinearPCMFormatFlagsSampleFractionShift    = 7,
    /// <summary>
    /// <c>number_fractional_bits = (mFormatFlags &amp; kLinearPCMFormatFlagsSampleFractionMask) >> kLinearPCMFormatFlagsSampleFractionShift</c>
    /// </summary>
    kLinearPCMFormatFlagsSampleFractionMask     = (0x3F << unchecked((int)kLinearPCMFormatFlagsSampleFractionShift)),
}