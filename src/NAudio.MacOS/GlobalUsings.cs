
/*
typedef OSStatus
(*AudioObjectPropertyListenerProc)( AudioObjectID                       inObjectID,
                                    UInt32                              inNumberAddresses,
                                    const AudioObjectPropertyAddress*   inAddresses,
                                    void* __nullable                    inClientData);
*/

global using unsafe AudioObjectPropertyListenerProc = delegate* unmanaged[Cdecl]<NAudio.MacOS.CoreAudio.Interop.AudioObjectID, uint, nint, nint, int /* OSStatus */>;

/*
typedef OSStatus
(*AudioDeviceIOProc)(   AudioObjectID           inDevice,
                        const AudioTimeStamp*   inNow,
                        const AudioBufferList*  inInputData,
                        const AudioTimeStamp*   inInputTime,
                        AudioBufferList*        outOutputData,
                        const AudioTimeStamp*   inOutputTime,
                        void* __nullable        inClientData);
*/

global using unsafe AudioDeviceIOProc = delegate* unmanaged[Cdecl]<NAudio.MacOS.CoreAudio.Interop.AudioObjectID, nint, nint, nint, nint, nint, nint, int /* OSStatus */>;

/*
typedef OSStatus
(*AudioConverterComplexInputDataProc)(  AudioConverterRef               inAudioConverter,
                                        UInt32 *                        ioNumberDataPackets,
                                        AudioBufferList *               ioData,
                                        AudioStreamPacketDescription * __nullable * __nullable outDataPacketDescription,
                                        void * __nullable               inUserData);
*/
// Second parameter is rather uint* to avoid typecasts at run-time - it will be used anyway.
global using unsafe AudioConverterComplexInputDataProc = delegate* unmanaged[Cdecl]<System.IntPtr, uint*, nint, nint, nint, int /* OSStatus */>;

/*!
    @typedef	AudioFile_ReadProc
    @abstract   A callback for reading data. used with AudioFileOpenWithCallbacks or AudioFileInitializeWithCallbacks.
    @discussion a function that will be called when AudioFile needs to read data.
    @param      inClientData	A pointer to the client data as set in the inClientData parameter to AudioFileXXXWithCallbacks.
    @param      inPosition		An offset into the data from which to read.
    @param      requestCount	The number of bytes to read.
    @param      buffer			The buffer in which to put the data read.
    @param      actualCount		The callback should set this to the number of bytes successfully read.
    @result						The callback should return noErr on success, or an appropriate error code on failure.

typedef OSStatus (*AudioFile_ReadProc)(
								void *		inClientData,
								SInt64		inPosition, 
								UInt32		requestCount,
								void *		buffer, 
								UInt32 *	actualCount);
*/
global using unsafe AudioFile_ReadProc = delegate* unmanaged[Cdecl]<System.IntPtr, long, uint, nint, uint*, int /*OSStatus*/>;

/*!
    @typedef	AudioFile_WriteProc
    @abstract   A callback for writing data. used with AudioFileOpenWithCallbacks or AudioFileInitializeWithCallbacks.
    @discussion a function that will be called when AudioFile needs to write data.
    @param      inClientData	A pointer to the client data as set in the inClientData parameter to AudioFileXXXWithCallbacks.
    @param      inPosition		An offset into the data from which to read.
    @param      requestCount	The number of bytes to write.
    @param      buffer			The buffer containing the data to write.
    @param      actualCount		The callback should set this to the number of bytes successfully written.
    @result						The callback should return noErr on success, or an appropriate error code on failure.

typedef OSStatus (*AudioFile_WriteProc)(
								void * 		inClientData,
								SInt64		inPosition, 
								UInt32		requestCount, 
								const void *buffer, 
								UInt32    * actualCount);
*/
global using unsafe AudioFile_WriteProc = delegate* unmanaged[Cdecl]<System.IntPtr, long, uint, nint, uint*, int /*OSStatus*/>;

/*!
    @typedef	AudioFile_GetSizeProc
    @abstract   A callback for getting the size of the file data. used with AudioFileOpenWithCallbacks or AudioFileInitializeWithCallbacks.
    @discussion a function that will be called when AudioFile needs to determine the size of the file data. This size is for all of the 
				data in the file, not just the audio data.
    @param      inClientData	A pointer to the client data as set in the inClientData parameter to AudioFileXXXWithCallbacks.
    @result						The callback should return the size of the data.

typedef SInt64 (*AudioFile_GetSizeProc)(
								void * 		inClientData);
*/
global using unsafe AudioFile_GetSizeProc = delegate* unmanaged[Cdecl]<System.IntPtr, long>;

/*!
    @typedef	AudioFile_SetSizeProc
    @abstract   A callback for setting the size of the file data. used with AudioFileOpenWithCallbacks or AudioFileInitializeWithCallbacks.
    @discussion a function that will be called when AudioFile needs to set the size of the file data. This size is for all of the 
				data in the file, not just the audio data. This will only be called if the file is written to.
    @param      inClientData	A pointer to the client data as set in the inClientData parameter to AudioFileXXXWithCallbacks.
    @result						The callback should return the size of the data.
 
typedef OSStatus (*AudioFile_SetSizeProc)(
								void *		inClientData,
								SInt64		inSize);
*/
global using unsafe AudioFile_SetSizeProc = delegate* unmanaged[Cdecl]<System.IntPtr, long, int /*OSStatus*/>;

/*
#if __LLP64__
typedef unsigned long long CFHashCode;
#else
typedef unsigned long CFHashCode;
#endif
*/
global using CFHashCode = System.Runtime.InteropServices.CULong;

/*
#if __LLP64__
typedef signed long long CFIndex;
#else
typedef signed long CFIndex;
#endif
*/
global using CFIndex = System.Runtime.InteropServices.CLong;
