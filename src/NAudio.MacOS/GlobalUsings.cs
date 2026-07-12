
/*
typedef OSStatus
(*AudioObjectPropertyListenerProc)( AudioObjectID                       inObjectID,
                                    UInt32                              inNumberAddresses,
                                    const AudioObjectPropertyAddress*   inAddresses,
                                    void* __nullable                    inClientData);
*/

global using unsafe AudioObjectPropertyListenerProc = delegate* unmanaged[MemberFunction]<NAudio.MacOS.CoreAudio.Interop.AudioObjectID, uint, System.IntPtr, System.IntPtr, int /* OSStatus */>;

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

global using unsafe AudioDeviceIOProc = delegate* unmanaged[MemberFunction]<NAudio.MacOS.CoreAudio.Interop.AudioObjectID, nint, nint, nint, nint, nint, nint, int /* OSStatus */>;
