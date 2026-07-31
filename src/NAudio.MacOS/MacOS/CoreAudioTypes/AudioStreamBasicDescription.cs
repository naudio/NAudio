// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// This structure encapsulates all the information for describing 
/// the basic format properties of a stream of audio data.
/// </summary>
/// <remarks>
/// This structure is sufficient to describe any constant bit rate format that has
/// channels that are the same size. Extensions are required for variable bit rate
/// data and for constant bit rate data where the channels have unequal sizes.
/// However, where applicable, the appropriate fields will be filled out correctly
/// for these kinds of formats (the extra data is provided via separate properties).
/// In all fields, a value of 0 indicates that the field is either unknown, not
/// applicable or otherwise is inapproprate for the format and should be ignored.
/// Note that 0 is still a valid value for most formats in the mFormatFlags field.
///
/// In audio data a frame is one sample across all channels. In non-interleaved
/// audio, the per frame fields identify one channel. In interleaved audio, the per
/// frame fields identify the set of n channels. In uncompressed audio, a Packet is
/// one frame, (mFramesPerPacket == 1). In compressed audio, a Packet is an
/// indivisible chunk of compressed data, for example an AAC packet will contain
/// 1024 sample frames.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioStreamBasicDescription
{
    /// <summary>
    /// The number of sample frames per second of the data in the stream.
    /// </summary>
    public double mSampleRate;
    /// <summary>
    /// The AudioFormatID indicating the general kind of data in the stream.
    /// </summary>
    public AudioFormatID mFormatID;
    /// <summary>
    /// The AudioFormatFlags for the format indicated by mFormatID.
    /// </summary>
    public AudioFormatFlags mFormatFlags;
    /// <summary>
    /// The number of bytes in a packet of data.
    /// </summary>
    public UInt32 mBytesPerPacket;
    /// <summary>
    /// The number of sample frames in each packet of data.
    /// </summary>
    public UInt32 mFramesPerPacket;
    /// <summary>
    /// The number of bytes in a single sample frame of data.
    /// </summary>
    public UInt32 mBytesPerFrame;
    /// <summary>
    /// The number of channels in each frame of data.
    /// </summary>
    public UInt32 mChannelsPerFrame;
    /// <summary>
    /// The number of bits of sample data for each channel in a frame of data.
    /// </summary>
    public UInt32 mBitsPerChannel;
    /// <summary>
    /// Pads the structure out to force an even 8 byte alignment.
    /// </summary>
    public UInt32 mReserved;

    /// <summary>
    /// A method for checking if an ASBD indicates native endian linear PCM data.
    /// </summary>
    /// <param name="f">The ASBD to test.</param>
    /// <returns>The test outcome.</returns>
    public static bool IsAudioFormatNativeEndian(AudioStreamBasicDescription f)
    {
        bool nativeEndian = BitConverter.IsLittleEndian ?
            (f.mFormatFlags & AudioFormatFlags.kAudioFormatFlagIsBigEndian) == 0 :
            (f.mFormatFlags & AudioFormatFlags.kAudioFormatFlagIsBigEndian) != 0;
        return nativeEndian && (f.mFormatID == AudioFormatIDs.kAudioFormatLinearPCM);
    }

    /// <summary>
    /// A method for calculating the mFormatFlags for linear PCM data. Note
    /// that this function does not support specifying sample formats that are either
    /// unsigned integer or low-aligned.
    /// </summary>
    /// <param name="inValidBitsPerChannel">The number of valid bits in each sample.</param>
    /// <param name="inTotalBitsPerChannel">The total number of bits in each sample.</param>
    /// <param name="inIsFloat">Whether or not the samples are represented with floating point numbers.</param>
    /// <param name="inIsBigEndian">Whether the samples are big endian or little endian.</param>
    /// <param name="inIsNonInterleaved">Whether the channels are non-interleaved.</param>
    /// <returns>An <see cref="AudioFormatFlags"/> containing the format flags.</returns>
    public static AudioFormatFlags CalculateLPCMFlags(UInt32 inValidBitsPerChannel, UInt32 inTotalBitsPerChannel, bool inIsFloat, bool inIsBigEndian, bool inIsNonInterleaved = false)
            => (inIsFloat ? AudioFormatFlags.kAudioFormatFlagIsFloat : AudioFormatFlags.kAudioFormatFlagIsSignedInteger) |
               (inIsBigEndian ? AudioFormatFlags.kAudioFormatFlagIsBigEndian : 0) |
               ((inValidBitsPerChannel == inTotalBitsPerChannel) ? AudioFormatFlags.kAudioFormatFlagIsPacked : AudioFormatFlags.kAudioFormatFlagIsAlignedHigh) |
               (inIsNonInterleaved ? AudioFormatFlags.kAudioFormatFlagIsNonInterleaved : 0);

    /// <summary>
    /// A method for creating an AudioStreamBasicDescription to describe
    /// linear PCM data. Note that this function does not support specifying sample formats
    /// that are either unsigned integer or low-aligned.
    /// </summary>
    /// <param name="inSampleRate">The number of sample frames per second of the data in the stream.</param>
    /// <param name="inChannelsPerFrame">The number of channels in each frame of data.</param>
    /// <param name="inValidBitsPerChannel">The number of valid bits in each sample.</param>
    /// <param name="inTotalBitsPerChannel">The total number of bits in each sample.</param>
    /// <param name="inIsFloat">Whether or not the samples are represented with floating point numbers.</param>
    /// <param name="inIsBigEndian">Whether the samples are big endian or little endian.</param>
    /// <param name="inIsNonInterleaved">Whether the channels are non-interleaved.</param>
    /// <returns>A new <see cref="AudioStreamBasicDescription"/> instance.</returns>
    public static AudioStreamBasicDescription FillOutASBDForLPCM(
        double inSampleRate,
        UInt32 inChannelsPerFrame,
        UInt32 inValidBitsPerChannel,
        UInt32 inTotalBitsPerChannel,
        bool inIsFloat,
        bool inIsBigEndian,
        bool inIsNonInterleaved = false
    )
    {
        AudioStreamBasicDescription asbd = new();
        asbd.mSampleRate = inSampleRate;
        asbd.mFormatID = AudioFormatIDs.kAudioFormatLinearPCM;
        asbd.mFormatFlags = CalculateLPCMFlags(inValidBitsPerChannel, inTotalBitsPerChannel, inIsFloat, inIsBigEndian, inIsNonInterleaved);
        asbd.mBytesPerPacket = asbd.mBytesPerFrame = (inIsNonInterleaved ? 1U : inChannelsPerFrame) * (inTotalBitsPerChannel / 8U);
        asbd.mFramesPerPacket = 1U;
        asbd.mChannelsPerFrame = inChannelsPerFrame;
        asbd.mBitsPerChannel = inValidBitsPerChannel;
        return asbd;
    }
}