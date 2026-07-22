
using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// Provides all the required interoperability
/// methods for communicating with the Audio Toolbox framework.
/// Each ported API is defined on it's own special region.
/// </summary>
internal static partial class NativeMethods
{
    #region Audio Converter Functions

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

    /*
    extern OSStatus
    AudioConverterConvertBuffer(    AudioConverterRef               inAudioConverter,
                                    UInt32                          inInputDataSize,
                                    const void *                    inInputData,
                                    UInt32 *                        ioOutputDataSize,
                                    void *                          outOutputData)  API_AVAILABLE(macos(10.1), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Converts data from an input buffer to an output buffer.</summary>
    /// <param name="inAudioConverter">The AudioConverter to use.</param>
    /// <param name="inInputDataSize">The size of the buffer inInputData.</param>
    /// <param name="inInputData">The input audio data buffer.</param>
    /// <param name="ioOutputDataSize">
    /// On entry, the size of the buffer outOutputData. 
    /// On exit, the number of bytes written to outOutputData.
    /// </param>
    /// <param name="outOutputData">The output data buffer.</param>
    /// <returns>Produces a buffer of output data from an AudioConverter, using the supplied input buffer.</returns>
    /// <remarks>
    /// WARNING: this function will fail for any conversion where there is a
    /// variable relationship between the input and output data buffer sizes. This
    /// includes sample rate conversions and most compressed formats. In these cases,
    /// use AudioConverterFillComplexBuffer. Generally this function is only appropriate for
    /// PCM-to-PCM conversions where there is no sample rate conversion.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.1")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioConverterConvertBuffer(
        IntPtr inAudioConverter,
        uint inInputDataSize,
        IntPtr inInputData,
        ref uint ioOutputDataSize,
        IntPtr outOutputData
    );

    #endregion

    #region Extended Audio File Functions

    /*
    extern OSStatus
    ExtAudioFileOpenURL(		CFURLRef 					inURL,
                                ExtAudioFileRef __nullable * __nonnull outExtAudioFile)	API_AVAILABLE(macos(10.5), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Opens an audio file specified by a CFURLRef.</summary>
    /// <remarks>Allocates a new ExtAudioFileRef, for reading an existing audio file.</remarks>
    /// <param name="inURL">The audio file to read.</param>
    /// <param name="outExtAudioFile">On exit, a newly-allocated ExtAudioFileRef.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileOpenURL(IntPtr inURL, out IntPtr outExtAudioFile);

    /*
    extern OSStatus
    ExtAudioFileWrapAudioFileID(AudioFileID					inFileID,
                                Boolean						inForWriting,
                                ExtAudioFileRef __nullable * __nonnull outExtAudioFile)
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Wrap an AudioFileID in an ExtAudioFileRef.</summary>
    /// <remarks>
    /// Allocates a new ExtAudioFileRef which wraps an existing AudioFileID. The
    /// client is responsible for keeping the AudioFileID open until the
    /// ExtAudioFileRef is disposed. Disposing the ExtAudioFileRef will not close
    /// the AudioFileID when this Wrap API call is used, so the client is also
    /// responsible for closing the AudioFileID when finished with it.
    /// </remarks>
    /// <param name="inFileID">The AudioFileID to wrap.</param>
    /// <param name="inForWriting">True if the AudioFileID is a new file opened for writing.</param>
    /// <param name="outExtAudioFile">On exit, a newly-allocated ExtAudioFileRef.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileWrapAudioFileID(
        IntPtr inFileID,
        MacBoolean inForWriting,
        out IntPtr outExtAudioFile
    );

    /*
    extern OSStatus
    ExtAudioFileCreateWithURL(	CFURLRef							inURL,
                                AudioFileTypeID						inFileType,
                                const AudioStreamBasicDescription * inStreamDesc,
                                const AudioChannelLayout * __nullable inChannelLayout,
                                UInt32								inFlags,
                                ExtAudioFileRef __nullable * __nonnull outExtAudioFile)
                                                                                API_AVAILABLE(macos(10.5), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Create a new audio file.</summary>
    /// <param name="inURL">The URL of the new audio file.</param>
    /// <param name="inFileType">
    /// The type of file to create. 
    /// This is a constant from <see cref="AudioFileTypeIDs"/>, e.g. kAudioFileAIFFType. 
    /// Note that this is not an HFSTypeCode.
    /// </param>
    /// <param name="inStreamDesc">The format of the audio data to be written to the file.</param>
    /// <param name="inChannelLayout">
    /// The channel layout of the audio data. If non-null, this must be consistent
    /// with the number of channels specified by inStreamDesc.
    /// </param>
    /// <param name="flags">
    /// The same flags as are used with AudioFileCreateWithURL
    /// Can use these to control whether an existing file is overwritten (or not).
    /// </param>
    /// <param name="outExtAudioFile">On exit, a newly-allocated ExtAudioFileRef.</param>
    /// <returns>
    /// Creates a new audio file. <br />
    /// 
    /// If the file to be created is in an encoded format, it is permissible for the
    /// sample rate in inStreamDesc to be 0, since in all cases, the file's encoding
    /// AudioConverter may produce audio at a different sample rate than the source. The
    /// file will be created with the audio format actually produced by the encoder.
    /// </returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileCreateWithURL(
        IntPtr inURL,
        AudioFileTypeID inFileType,
        in AudioStreamBasicDescription inStreamDesc,
        in AudioChannelLayout inChannelLayout,
        AudioFileFlags flags,
        out IntPtr outExtAudioFile
    );

    /*
    extern OSStatus
    ExtAudioFileDispose(		ExtAudioFileRef				inExtAudioFile)		
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Close the file and dispose the object.</summary>
    /// <remarks>Closes the file and deletes the object.</remarks>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileDispose(IntPtr inExtAudioFile);

    /*
    extern OSStatus
    ExtAudioFileRead(			ExtAudioFileRef			inExtAudioFile,
                                UInt32 *				ioNumberFrames,
                                AudioBufferList *		ioData)					
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Perform a synchronous sequential read.</summary>
    /// <remarks>
    /// If the file has a client data format, then the audio data from the file is
    /// translated from the file data format to the client format, via the
    /// ExtAudioFile's internal AudioConverter. <br />
    /// 
    /// (Note that the use of sequential reads/writes means that an ExtAudioFile must
    /// not be read on multiple threads; clients wishing to do this should use the
    /// lower-level AudioFile API set).
    /// </remarks>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="ioNumberFrames">
    /// On entry, ioNumberFrames is the number of frames to be read from the file.
    /// On exit, it is the number of frames actually read. A number of factors may
    /// cause a fewer number of frames to be read, including the supplied buffers
    /// not being large enough, and internal optimizations. If 0 frames are
    /// returned, however, this indicates that end-of-file was reached.
    /// </param>
    /// <param name="ioData">Buffer(s) into which the audio data is read.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileRead(
        IntPtr inExtAudioFile,
        ref uint ioNumberFrames,
        ref AudioBufferList ioData
    );

    /*
    extern OSStatus
    ExtAudioFileWrite(			ExtAudioFileRef			inExtAudioFile,
                                UInt32					inNumberFrames,
                                const AudioBufferList * ioData)					
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Perform a synchronous sequential write.</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="inNumberFrames">The number of frames to write.</param>
    /// <param name="ioData">The buffer(s) from which audio data is written to the file.</param>
    /// <returns>An OSStatus error code.</returns>
    /// <remarks>
    /// If the file has a client data format, then the audio data in ioData is
    /// translated from the client format to the file data format, via the
    /// ExtAudioFile's internal AudioConverter.
    /// </remarks>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileWrite(
        IntPtr inExtAudioFile,
        uint inNumberFrames,
        in AudioBufferList ioData
    );

    /*
    extern OSStatus
    ExtAudioFileSeek(			ExtAudioFileRef			inExtAudioFile,
                                SInt64					inFrameOffset)			
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Seek to a specific frame position.</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="inFrameOffset">
    /// The desired seek position, in sample frames, relative to the beginning of
    /// the file. This is specified in the sample rate and frame count of the file's format
    /// (not the client format)
    /// </param>
    /// <returns>An OSStatus error code.</returns>
    /// <remarks>
    /// Sets the file's read position to the specified sample frame number. The next call
    /// to ExtAudioFileRead will return samples from precisely this location, even if it
    /// is located in the middle of a packet.
    /// 
    /// This function's behavior with files open for writing is currently undefined.
    /// </remarks>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileSeek(IntPtr inExtAudioFile, long inFrameOffset);

    /*
    extern OSStatus
    ExtAudioFileTell(			ExtAudioFileRef			inExtAudioFile,
                                SInt64 *				outFrameOffset)			
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Return the file's read/write position.</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="outFrameOffset">
    /// On exit, the file's current read/write position in sample frames. This is specified in the 
    /// sample rate and frame count of the file's format (not the client format)
    /// </param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileTell(
        IntPtr inExtAudioFile,
        out long outFrameOffset
    );

    /*
    extern OSStatus
    ExtAudioFileGetPropertyInfo(ExtAudioFileRef			inExtAudioFile,
                                ExtAudioFilePropertyID	inPropertyID,
                                UInt32 * __nullable		outSize,
                                Boolean * __nullable	outWritable)
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Get information about a property</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="inPropertyID">The property being queried.</param>
    /// <param name="outSize">On exit, this is set to the size of the property's value.</param>
    /// <param name="outWritable">On exit, this indicates whether the property value is settable.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileGetPropertyInfo(
        IntPtr inExtAudioFile,
        ExtAudioFilePropertyID inPropertyID,
        out uint outSize,
        out MacBoolean outWritable
    );

    /*
    extern OSStatus
    ExtAudioFileGetProperty(	ExtAudioFileRef			inExtAudioFile,
                                ExtAudioFilePropertyID	inPropertyID,
                                UInt32 *				ioPropertyDataSize,
                                void *					outPropertyData)
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Get a property value.</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="inPropertyID">The property being fetched.</param>
    /// <param name="ioPropertyDataSize">
    /// On entry, the size (in bytes) of the memory pointed to by outPropertyData.
    /// On exit, the actual size of the property data returned.
    /// </param>
    /// <param name="outPropertyData">The value of the property is copied to the memory this points to.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileGetProperty(
        IntPtr inExtAudioFile,
        ExtAudioFilePropertyID inPropertyID,
        ref uint ioPropertyDataSize,
        IntPtr outPropertyData
    );

    /*
    extern OSStatus
    ExtAudioFileSetProperty(	ExtAudioFileRef			inExtAudioFile,
                                ExtAudioFilePropertyID	inPropertyID,
                                UInt32					inPropertyDataSize,
                                const void *			inPropertyData)			
                                                                                API_AVAILABLE(macos(10.4), ios(2.1), watchos(2.0), tvos(9.0));
    */

    /// <summary>Set a property value.</summary>
    /// <param name="inExtAudioFile">The extended audio file object.</param>
    /// <param name="inPropertyID">The property being set.</param>
    /// <param name="inPropertyDataSize">The size of the property data, in bytes.</param>
    /// <param name="inPropertyData">Points to the property's new value.</param>
    /// <returns>An OSStatus error code.</returns>
    [SupportedOSPlatform("ios2.1")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int ExtAudioFileSetProperty(
        IntPtr inExtAudioFile,
        ExtAudioFilePropertyID inPropertyID,
        uint inPropertyDataSize,
        IntPtr inPropertyData
    );

    #endregion

    #region Audio File Functions

    /*
    extern OSStatus	
    AudioFileInitializeWithCallbacks (	
                            void *								inClientData, 
                            AudioFile_ReadProc					inReadFunc, 
                            AudioFile_WriteProc					inWriteFunc, 
                            AudioFile_GetSizeProc				inGetSizeFunc,
                            AudioFile_SetSizeProc				inSetSizeFunc,
                            AudioFileTypeID						inFileType,
                            const AudioStreamBasicDescription	*inFormat,
                            AudioFileFlags						inFlags,
                            AudioFileID	__nullable * __nonnull	outAudioFile)	API_AVAILABLE(macos(10.3), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>
    /// Wipe clean an existing file. 
    /// ou provide callbacks that the AudioFile API will use to get the data.
    /// </summary>
    /// <param name="inClientData">a constant that will be passed to your callbacks.</param>
    /// <param name="inReadFunc">a function that will be called when AudioFile needs to read data.</param>
    /// <param name="inWriteFunc">a function that will be called when AudioFile needs to write data.</param>
    /// <param name="inGetSizeFunc">a function that will be called when AudioFile needs to know the file size.</param>
    /// <param name="inSetSizeFunc">a function that will be called when AudioFile needs to set the file size.</param>
    /// <param name="inFileType">an AudioFileTypeID indicating the type of audio file to which to initialize the file.</param>
    /// <param name="inFormat">an AudioStreamBasicDescription describing the data format that will be added to the audio file.</param>
    /// <param name="inFlags">flags for creating/opening the file. Currently zero.</param>
    /// <param name="outAudioFile">upon success, an AudioFileID that can be used for subsequent AudioFile calls.</param>
    /// <returns>returns noErr if successful.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.3")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioFileInitializeWithCallbacks(
        IntPtr inClientData,
        AudioFile_ReadProc inReadFunc,
        AudioFile_WriteProc inWriteFunc,
        AudioFile_GetSizeProc inGetSizeFunc,
        AudioFile_SetSizeProc inSetSizeFunc,
        AudioFileTypeID inFileType,
        in AudioStreamBasicDescription inFormat,
        AudioFileFlags inFlags,
        out IntPtr outAudioFile
    );

    /*
    extern OSStatus	
    AudioFileOpenWithCallbacks (
                    void *								inClientData,
                    AudioFile_ReadProc					inReadFunc,
                    AudioFile_WriteProc __nullable		inWriteFunc,
                    AudioFile_GetSizeProc				inGetSizeFunc,
                    AudioFile_SetSizeProc __nullable	inSetSizeFunc,
                    AudioFileTypeID						inFileTypeHint,
                    AudioFileID	__nullable * __nonnull	outAudioFile)				API_AVAILABLE(macos(10.3), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>
    /// Open an existing file. 
    /// You provide callbacks that the AudioFile API 
    /// will use to get the data.
    /// </summary>
    /// <param name="inClientData">a constant that will be passed to your callbacks.</param>
    /// <param name="inReadFunc">a function that will be called when AudioFile needs to read data.</param>
    /// <param name="inWriteFunc">a function that will be called when AudioFile needs to write data.</param>
    /// <param name="inGetSizeFunc">a function that will be called when AudioFile needs to know the file size.</param>
    /// <param name="inSetSizeFunc">a function that will be called when AudioFile needs to set the file size.</param>
    /// <param name="inFileTypeHint">
    /// For files which have no filename extension and whose type cannot be easily or
    /// uniquely determined from the data (ADTS,AC3), this hint can be used to indicate the file type. 
    /// Otherwise you can pass zero for this. The hint is only used on OS versions 10.3.1 or greater.
    /// For OS versions prior to that, opening files of the above description will fail.
    /// </param>
    /// <param name="outAudioFile">upon success, an AudioFileID that can be used for subsequent AudioFile calls.</param>
    /// <returns>returns noErr if successful.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.3")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioFileOpenWithCallbacks(
        IntPtr inClientData,
        AudioFile_ReadProc inReadFunc,
        AudioFile_WriteProc inWriteFunc,
        AudioFile_GetSizeProc inGetSizeFunc,
        AudioFile_SetSizeProc inSetSizeFunc,
        AudioFileTypeID inFileTypeHint,
        out IntPtr outAudioFile
    );

    /*
    extern OSStatus
    AudioFileClose	(AudioFileID		inAudioFile)							API_AVAILABLE(macos(10.2), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Close an existing audio file.</summary>
    /// <param name="inAudioFile">an AudioFileID.</param>
    /// <returns>returns noErr if successful.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.2")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioFileClose(IntPtr inAudioFile);

    #endregion
}
