
using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.MacOS.AudioToolbox.Interop;

internal static partial class NativeMethods
{
    /*
    extern OSStatus
    AudioConverterNew(      const AudioStreamBasicDescription * inSourceFormat,
                            const AudioStreamBasicDescription * inDestinationFormat,
                            AudioConverterRef __nullable * __nonnull outAudioConverter)      API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Create a new AudioConverter.</summary>
    /// <param name="inSourceFormat">The format of the source audio to be converted.</param>
    /// <param name="inDestinationFormat">The destination format to which the audio is to be converted.</param>
    /// <param name="outAudioConverter">On successful return, points to a new AudioConverter instance.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// For a pair of linear PCM formats, the following conversions
    ///	are supported:
	/// <list type="bullet">
	///     <item>
    ///           addition and removal of channels, when the stream descriptions'
	///           mChannelsPerFrame does not match. Channels may also be reordered and removed
	///           using the kAudioConverterChannelMap property.
    ///     </item>
	///     <item>sample rate conversion</item>
	///     <item>
    ///         interleaving/deinterleaving, when the stream descriptions' (mFormatFlags &amp;
	///         kAudioFormatFlagIsNonInterleaved) does not match.
    ///     </item>
	///     <item>
    ///         conversion between any pair of the following formats:
    ///         <list type="bullet">
    ///             <item>8 bit integer, signed or unsigned</item>
    ///	            <item>16, 24, or 32-bit integer, big- or little-endian. Other integral
    ///	            bit depths, if high-aligned and non-packed, are also supported</item>
    ///	            <item>32 and 64-bit float, big- or little-endian.</item>
    ///         </list>
    ///     </item>
	/// </list>
	///
	/// Also, encoding and decoding between linear PCM and compressed formats is
	/// supported. Functions in AudioToolbox/AudioFormat.h return information about the
	/// supported formats. When using a codec, you can use any supported PCM format (as
	/// above); the converter will perform any necessary additional conversion between
	/// your PCM format and the one created or consumed by the codec. <br /> <br />
	///
	/// Note that AudioConverter may change the formats to correct any
	/// inconsistent or erroneous values.  The actual formats expected and used
	/// by the newly created AudioConverter can be obtained by getting the
    ///	properties `kAudioConverterCurrentInputStreamDescription` and
	/// `kAudioConverterCurrentOutputStreamDescription` from it.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterNew(in AudioStreamBasicDescription inSourceFormat, in AudioStreamBasicDescription inDestinationFormat, out IntPtr outAudioConverter);

    /*
    extern OSStatus
    AudioConverterDispose(  AudioConverterRef   inAudioConverter)                   API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Destroy an AudioConverter.</summary>
    /// <param name="inAudioConverter">The AudioConverter to dispose.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterDispose(IntPtr inAudioConverter);

    /*!
        @function   AudioConverterReset
        @abstract   Reset an AudioConverter

        @param      inAudioConverter
                        The AudioConverter to reset.
        @result     An OSStatus result code.
        
        Should be called whenever there is a discontinuity in the source audio stream
        being provided to the converter. This will flush any internal buffers in the
        converter.
    */

    /*
    extern OSStatus
    AudioConverterReset(    AudioConverterRef   inAudioConverter)                   API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Reset an AudioConverter</summary>
    /// <param name="inAudioConverter">The AudioConverter to reset.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Should be called whenever there is a discontinuity in the source audio stream
    /// being provided to the converter. This will flush any internal buffers in the
    /// converter.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterReset(IntPtr inAudioConverter);

    /*
    extern OSStatus
    AudioConverterGetPropertyInfo(  AudioConverterRef           inAudioConverter,
                                    AudioConverterPropertyID    inPropertyID,
                                    UInt32 * __nullable         outSize,
                                    Boolean * __nullable        outWritable)        API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Returns information about an AudioConverter property.</summary>
    /// <param name="inAudioConverter">The AudioConverter to query.</param>
    /// <param name="inPropertyID">The property to query.</param>
    /// <param name="outSize">On exit, the maximum size of the property value in bytes.</param>
    /// <param name="outWritable">On exit, indicates whether the property value is writable.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterGetPropertyInfo(IntPtr inAudioConverter, AudioConverterPropertyID inPropertyID, out uint outSize, out MacBoolean outWritable);

    /*
    extern OSStatus
    AudioConverterGetProperty(  AudioConverterRef           inAudioConverter,
                                AudioConverterPropertyID    inPropertyID,
                                UInt32 *                    ioPropertyDataSize,
                                void *                      outPropertyData)        API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Returns an AudioConverter property value.</summary>
    /// <param name="inAudioConverter">The AudioConverter to query.</param>
    /// <param name="inPropertyID">The property to fetch.</param>
    /// <param name="ioPropertyDataSize">
    /// On entry, the size of the memory pointed to by outPropertyData. On 
    /// successful exit, the size of the property value.
    /// </param>
    /// <param name="outPropertyData">On exit, the property value.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterGetProperty(IntPtr inAudioConverter, AudioConverterPropertyID inPropertyID, ref uint ioPropertyDataSize, IntPtr outPropertyData);

    /*
    extern OSStatus
    AudioConverterSetProperty(  AudioConverterRef           inAudioConverter,
                                AudioConverterPropertyID    inPropertyID,
                                UInt32                      inPropertyDataSize,
                                const void *                inPropertyData)         API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Sets an AudioConverter property value.</summary>
    /// <param name="inAudioConverter">The AudioConverter to modify.</param>
    /// <param name="inPropertyID">The property to set.</param>
    /// <param name="inPropertyDataSize">The size in bytes of the property value.</param>
    /// <param name="inPropertyData">Points to the new property value.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterSetProperty(IntPtr inAudioConverter, AudioConverterPropertyID inPropertyID, uint inPropertyDataSize, IntPtr inPropertyData);

    /*
    extern OSStatus
    AudioConverterFillComplexBuffer(    AudioConverterRef                   inAudioConverter,
                                        AudioConverterComplexInputDataProc  inInputDataProc,
                                        void * __nullable                   inInputDataProcUserData,
                                        UInt32 *                            ioOutputDataPacketSize,
                                        AudioBufferList *                   outOutputData,
                                        AudioStreamPacketDescription * __nullable outPacketDescription)
                                                                                    API_AVAILABLE(macos(10.2), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>
    /// Converts data supplied by an input callback function,
    /// supporting non-interleaved and packetized formats.
    /// </summary>
    /// <param name="inAudioConverter">The AudioConverter to use.</param>
    /// <param name="inInputDataProc">A callback function which supplies the input data.</param>
    /// <param name="inInputDataProcUserData">A value for the use of the callback function.</param>
    /// <param name="ioOutputDataPacketSize">
    /// On entry, the capacity of outOutputData expressed in packets in the 
    /// converter's output format. On exit, the number of packets of 
    /// converted data that were written to outOutputData.
    /// </param>
    /// <param name="outOutputData">
    /// The converted output data is written to this buffer. On entry, the buffers'
    /// mDataByteSize fields (which must all be the same) reflect buffer capacity.
    /// On exit, mDataByteSize is set to the number of bytes written.
    /// </param>
    /// <param name="outPacketDescription">
    /// If non-null, and the converter's output uses packet descriptions, then
    /// packet descriptions are written to this array. It must point to a memory
    /// block capable of holding *ioOutputDataPacketSize packet descriptions.
    /// (See AudioFormat.h for ways to determine whether an audio format
    /// uses packet descriptions).
    /// </param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Produces a buffer list of output data from an AudioConverter. The supplied input
    /// callback function is called whenever necessary. <br /> <br />
    /// 
    /// If the output format uses packet descriptions, such as most compressed formats where packets
    /// vary in size or duration, the caller is expected to provide a buffer for holding packet descriptions,
    /// pointed to by outPacketDescription.  The array must have the capacity to hold a packet description
    /// for each output packet that may be written.  A packet description array is expected even if only
    /// a single output packet is to be written.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.2")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioConverterFillComplexBuffer(
        IntPtr inAudioConverter,
        AudioConverterComplexInputDataProc inInputDataProc,
        IntPtr inInputDataProcUserData,
        ref uint ioOutputDataPacketSize,
        ref AudioBufferList outOutputData,
        IntPtr outPacketDescription
    );



}
