
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
