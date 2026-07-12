/*==================================================================================================
     File:       CoreAudio/AudioHardware.h

     Contains:   API for communicating with audio hardware.

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

#pragma warning disable IDE0055 // We want the native methods to have a consistent view.

using System;
using System.Runtime.Versioning;
using NAudio.MacOS.CoreAudioTypes;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreAudio.Interop;

internal static partial class NativeMethods
{
    // extern Boolean AudioObjectHasProperty(AudioObjectID inObjectID, const AudioObjectPropertyAddress* inAddress) __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectHasProperty")]
    private static partial MacBoolean AudioObjectHasProperty_Native(AudioObjectID inObjectID, in AudioObjectPropertyAddress inAddress);

    /// <summary>
    /// Queries an AudioObject about whether or not it has the given property.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being queried.</param>
    /// <returns>A Boolean indicating whether or not the AudioObject has the given property.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static bool AudioObjectHasProperty(AudioObjectID inObjectID, AudioObjectPropertyAddress inAddress)
        => AudioObjectHasProperty_Native(inObjectID, in inAddress) != MacBoolean.False;

    // extern OSStatus
    // AudioObjectIsPropertySettable(  AudioObjectID                       inObjectID,
    //                        const AudioObjectPropertyAddress*   inAddress,
    //                        Boolean*                            outIsSettable)                  __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectIsPropertySettable")]
    private static partial int AudioObjectIsPropertySettable_Native(AudioObjectID inObjectID, in AudioObjectPropertyAddress inAddress, out MacBoolean outIsSettable);

    /// <summary>
    /// Queries an AudioObject about whether or not the given property can be set using AudioObjectSetPropertyData.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being queried.</param>
    /// <param name="outIsSettable">A Boolean indicating whether or not the property can be set.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static int AudioObjectIsPropertySettable(AudioObjectID inObjectID, AudioObjectPropertyAddress inAddress, out bool outIsSettable)
    {
        int status = AudioObjectIsPropertySettable_Native(inObjectID, in inAddress, out MacBoolean isSettableIntermediate);
        outIsSettable = isSettableIntermediate != MacBoolean.False;
        return status;
    }

    // extern OSStatus
    // AudioObjectGetPropertyDataSize( AudioObjectID                       inObjectID,
    //                         const AudioObjectPropertyAddress*   inAddress,
    //                         UInt32                              inQualifierDataSize,
    //                         const void* __nullable              inQualifierData,
    //                         UInt32*                             outDataSize)                    __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectGetPropertyDataSize")]
    private static partial int AudioObjectGetPropertyDataSize_Native(
                            AudioObjectID                 inObjectID,
                            in AudioObjectPropertyAddress inAddress,
                            UInt32                        inQualifierDataSize,
                            [AllowNull] IntPtr            inQualifierData,
                            out UInt32                    outDataSize
    );

    /// <summary>
    /// Queries an AudioObject to find the size of the data for the given property.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the qualifier data buffer.</typeparam>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being queried.</param>
    /// <param name="inQualifierData">
    /// A buffer of data to be used in determining the data of the property being
    /// queried. Note that not all properties require qualification, in which case
    /// this value will be <see cref="Span{T}.Empty"/>.
    /// </param>
    /// <param name="outDataSize">A UInt32 indicating how many bytes the data for the given property occupies.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public unsafe static int AudioObjectGetPropertyDataSize<T>(
        AudioObjectID               inObjectID,
        AudioObjectPropertyAddress  inAddress,
        Span<T>                     inQualifierData,
        out UInt32                  outDataSize
    ) where T : unmanaged
    {
        if (inQualifierData.IsEmpty)
        {
            return AudioObjectGetPropertyDataSize(inObjectID, inAddress, out outDataSize);
        }
        else
        {
            fixed (T* pData = inQualifierData)
            {
                return AudioObjectGetPropertyDataSize_Native(
                    inObjectID,
                    in inAddress,
                    (uint)(inQualifierData.Length * (long)sizeof(T)),
                    new(pData),
                    out outDataSize
                );
            }
        }
    }

    /// <summary>
    /// Queries an AudioObject to find the size of the data for the given property.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being queried.</param>
    /// <param name="outDataSize">A UInt32 indicating how many bytes the data for the given property occupies.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static int AudioObjectGetPropertyDataSize(
        AudioObjectID              inObjectID,
        AudioObjectPropertyAddress inAddress,
        out UInt32                 outDataSize
    ) => AudioObjectGetPropertyDataSize_Native(inObjectID, in inAddress, 0U, IntPtr.Zero, out outDataSize);

    /*
    extern OSStatus
    AudioObjectGetPropertyData( AudioObjectID                       inObjectID,
                                const AudioObjectPropertyAddress*   inAddress,
                                UInt32                              inQualifierDataSize,
                                const void* __nullable              inQualifierData,
                                UInt32*                             ioDataSize,
                                void*                               outData)                            __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    */
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectGetPropertyData")]
    private static partial int AudioObjectGetPropertyData_Native( 
                                AudioObjectID                 inObjectID,
                                in AudioObjectPropertyAddress inAddress,
                                UInt32                        inQualifierDataSize,
                                [AllowNull] IntPtr            inQualifierData,
                                ref UInt32                    ioDataSize,
                                IntPtr                        outData
    );

    /// <summary>
    /// Queries an AudioObject to get the data of the given 
    /// property and places it in the provided buffer.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An <see cref="AudioObjectPropertyAddress"/> indicating which property is being queried.</param>
    /// <param name="ioDataSize">
    /// A UInt32 which on entry indicates the size of the buffer pointed to by
    /// <paramref name="outData"/> and on exit indicates how much of the buffer was used.
    /// </param>
    /// <param name="outData">The buffer into which the AudioObject will put the data for the given property.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static int AudioObjectGetPropertyData(
        AudioObjectID              inObjectID,
        AudioObjectPropertyAddress inAddress,
        ref UInt32                 ioDataSize,
        IntPtr                     outData
    ) => AudioObjectGetPropertyData_Native(inObjectID, in inAddress, 0U, IntPtr.Zero, ref ioDataSize, outData);

    /// <summary>
    /// Queries an AudioObject to get the data of the given 
    /// property and places it in the provided buffer.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to query.</param>
    /// <param name="inAddress">An <see cref="AudioObjectPropertyAddress"/> indicating which property is being queried.</param>
    /// <param name="inQualifierData"> A buffer of data to be used in determining the data of the property being
    /// queried. Note that not all properties require qualification, in which case
    /// this value will be <see cref="Span{T}.Empty"/>.</param>
    /// <param name="ioDataSize">
    /// A UInt32 which on entry indicates the size of the buffer pointed to by
    /// <paramref name="outData"/> and on exit indicates how much of the buffer was used.
    /// </param>
    /// <param name="outData">The buffer into which the AudioObject will put the data for the given property.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public unsafe static int AudioObjectGetPropertyData<T>(
        AudioObjectID              inObjectID,
        AudioObjectPropertyAddress inAddress,
        Span<T>                    inQualifierData,
        ref UInt32                 ioDataSize,
        IntPtr                     outData
    ) where T : unmanaged
    {
        if (inQualifierData.IsEmpty)
        {
            return AudioObjectGetPropertyData(inObjectID, inAddress, ref ioDataSize, outData);
        }
        else
        {
            fixed (T* pData = inQualifierData)
            {
                return AudioObjectGetPropertyData_Native(
                    inObjectID,
                    in inAddress,
                    (uint)(inQualifierData.Length * (long)sizeof(T)),
                    new(pData),
                    ref ioDataSize,
                    outData
                );
            }
        }
    }

    /*
    extern OSStatus
    AudioObjectSetPropertyData( AudioObjectID                       inObjectID,
                                const AudioObjectPropertyAddress*   inAddress,
                                UInt32                              inQualifierDataSize,
                                const void* __nullable              inQualifierData,
                                UInt32                              inDataSize,
                                const void*                         inData)                             __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    */
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectSetPropertyData")]
    private static partial int AudioObjectSetPropertyData_Native(
        AudioObjectID                   inObjectID,
        in AudioObjectPropertyAddress   inAddress,
        UInt32                          inQualifierDataSize,
        [AllowNull] IntPtr              inQualifierData,
        UInt32                          inDataSize,
        IntPtr                          inData
    );

    /// <summary>
    /// Tells an AudioObject to change the value of the given property using the provided data.
    /// </summary>
    /// <remarks>
    /// Note that the value of the property should not be considered changed until the
    /// HAL has called the listeners as many properties values are changed
    /// asynchronously.
    /// </remarks>
    /// <param name="inObjectID">The AudioObject to change.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being changed.</param>
    /// <param name="inDataSize">A UInt32 indicating the size of the buffer pointed to by <paramref name="inData"/>.</param>
    /// <param name="inData">The buffer containing the data to be used to change the property's value.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static int AudioObjectSetPropertyData(
        AudioObjectID                inObjectID,
        AudioObjectPropertyAddress   inAddress,
        UInt32                       inDataSize,
        IntPtr                       inData
    ) => AudioObjectSetPropertyData_Native(inObjectID, in inAddress, 0U, IntPtr.Zero, inDataSize, inData);


    /// <summary>
    /// Tells an AudioObject to change the value of the given property using the provided data.
    /// </summary>
    /// <remarks>
    /// Note that the value of the property should not be considered changed until the
    /// HAL has called the listeners as many properties values are changed
    /// asynchronously.
    /// </remarks>
    /// <param name="inObjectID">The AudioObject to change.</param>
    /// <param name="inAddress">An AudioObjectPropertyAddress indicating which property is being changed.</param>
    /// <param name="inDataSize">A UInt32 indicating the size of the buffer pointed to by <paramref name="inData"/>.</param>
    /// <param name="inData">The buffer containing the data to be used to change the property's value.</param>
    /// <param name="inQualifierData">
    /// A buffer of data to be used in determining the data of the property being
    /// queried. Note that not all properties require qualification, in which case
    /// this value will be <see cref="Span{T}.Empty"/>.
    /// </param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static unsafe int AudioObjectSetPropertyData<T>(
        AudioObjectID                inObjectID,
        AudioObjectPropertyAddress   inAddress,
        Span<T>                      inQualifierData,
        UInt32                       inDataSize,
        IntPtr                       inData
    ) where T : unmanaged
    {
        if (inQualifierData.IsEmpty)
        {
            return AudioObjectSetPropertyData(inObjectID, inAddress, inDataSize, inData);
        }
        else
        {
            fixed (T* pData = inQualifierData)
            {
                return AudioObjectSetPropertyData_Native(
                    inObjectID,
                    inAddress,
                    (uint)(inQualifierData.Length * (long)sizeof(T)),
                    new(pData),
                    inDataSize,
                    inData
                );
            }
        }
    }

    /*
    extern OSStatus
    AudioObjectAddPropertyListener( AudioObjectID                       inObjectID,
                                    const AudioObjectPropertyAddress*   inAddress,
                                    AudioObjectPropertyListenerProc     inListener,
                                    void* __nullable                    inClientData)                   __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    */
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectAddPropertyListener")]
    private static unsafe partial int AudioObjectAddPropertyListener_Native(
        AudioObjectID                   inObjectID,
        in AudioObjectPropertyAddress   inAddress,
        AudioObjectPropertyListenerProc inListener,
        [AllowNull] IntPtr              inClientData
    );

    /// <summary>
    /// Registers the given AudioObjectPropertyListenerProc to 
    /// receive notifications when the given properties change.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to register the listener with.</param>
    /// <param name="inAddress">The AudioObjectPropertyAddresses indicating which property the listener should be notified about.</param>
    /// <param name="inListener">The AudioObjectPropertyListenerProc to call.</param>
    /// <param name="inClientData">A pointer to client data that is passed to the listener when it is called.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static unsafe int AudioObjectAddPropertyListener(
        AudioObjectID                   inObjectID,
        AudioObjectPropertyAddress      inAddress,
        AudioObjectPropertyListenerProc inListener,
        [AllowNull] IntPtr              inClientData
    ) => AudioObjectAddPropertyListener_Native(inObjectID, in inAddress, inListener, inClientData);

    /*
    extern OSStatus
    AudioObjectRemovePropertyListener( AudioObjectID                       inObjectID,
                                    const AudioObjectPropertyAddress*   inAddress,
                                    AudioObjectPropertyListenerProc     inListener,
                                    void* __nullable                    inClientData)                   __OSX_AVAILABLE_STARTING(__MAC_10_4, __IPHONE_2_0);
    */
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    [LibraryImport(MacLibraries.CoreAudio, EntryPoint = "AudioObjectRemovePropertyListener")]
    private static unsafe partial int AudioObjectRemovePropertyListener_Native(
        AudioObjectID                   inObjectID,
        in AudioObjectPropertyAddress   inAddress,
        AudioObjectPropertyListenerProc inListener,
        [AllowNull] IntPtr              inClientData
    );

    /// <summary>
    /// Unregisters the given AudioObjectPropertyListenerProc from 
    /// receiving notifications when the given properties change.
    /// </summary>
    /// <param name="inObjectID">The AudioObject to unregister the listener from.</param>
    /// <param name="inAddress">The AudioObjectPropertyAddress indicating from which property the listener should be removed.</param>
    /// <param name="inListener">The AudioObjectPropertyListenerProc being removed.</param>
    /// <param name="inClientData">A pointer to client data that is passed to the listener when it is called.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.4")]
    public static unsafe int AudioObjectRemovePropertyListener(
        AudioObjectID                   inObjectID,
        AudioObjectPropertyAddress      inAddress,
        AudioObjectPropertyListenerProc inListener,
        [AllowNull] IntPtr              inClientData
    ) => AudioObjectAddPropertyListener_Native(inObjectID, in inAddress, inListener, inClientData);

    /*
        extern OSStatus
        AudioDeviceCreateIOProcID(  AudioObjectID                               inDevice,
                                    AudioDeviceIOProc                           inProc,
                                    void* __nullable                            inClientData,
                                    AudioDeviceIOProcID __nullable * __nonnull  outIOProcID)  __OSX_AVAILABLE_STARTING(__MAC_10_5, __IPHONE_2_0);
    */

    /// <summary>
    /// Creates an AudioDeviceIOProcID from an AudioDeviceIOProc and a client data pointer.
    /// </summary>
    /// <remarks>AudioDeviceIOProcIDs allow for the client to register the same function pointer with a device multiple times</remarks>
    /// <param name="inDevice">The AudioDevice to register the IOProc with.</param>
    /// <param name="inProc">The AudioDeviceIOProc to register.</param>
    /// <param name="inClientData">A pointer to client data that is passed back to the IOProc when it is called.</param>
    /// <param name="outIOProcID">The newly created AudioDeviceIOProcID.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.CoreAudio)]
    public static unsafe partial int AudioDeviceCreateIOProcID(
        AudioObjectID inDevice,
        AudioDeviceIOProc inProc,
        IntPtr inClientData,
        out IntPtr outIOProcID
    );

    /*
        extern OSStatus
        AudioDeviceDestroyIOProcID( AudioObjectID       inDevice,
                                    AudioDeviceIOProcID inIOProcID)                                     __OSX_AVAILABLE_STARTING(__MAC_10_5, __IPHONE_2_0);

    */

    /// <summary>Destroys an AudioDeviceIOProcID.</summary>
    /// <remarks>AudioDeviceIOProcIDs allow for the client to register the same function pointer with a device multiple times</remarks>
    /// <param name="inDevice">The AudioDevice from which the ID came.</param>
    /// <param name="inIOProcID">The AudioDeviceIOProcID to get rid of.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.CoreAudio)]
    public static partial int AudioDeviceDestroyIOProcID(AudioObjectID inDevice, IntPtr inIOProcID);

    /*
        extern OSStatus
        AudioDeviceStart(   AudioObjectID                   inDevice,
                            AudioDeviceIOProcID __nullable  inProcID)                                                   __OSX_AVAILABLE_STARTING(__MAC_10_0, __IPHONE_2_0);

    */

    /// <summary>Starts IO for the given AudioDeviceIOProcID.</summary>
    /// <param name="inDevice">The AudioDevice to start the IOProc on.</param>
    /// <param name="inProcID">The AudioDeviceIOProcID to start. Note that this can be NULL, which starts
    /// the hardware regardless of whether or not there are any IOProcs registered.
    /// This is necessary if any of the AudioDevice's timing services are to be
    /// used. A balancing call to AudioDeviceStop with a NULL IOProc is required to
    /// stop the hardware.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.0")]
    [LibraryImport(MacLibraries.CoreAudio)]
    public static partial int AudioDeviceStart(AudioObjectID inDevice, [AllowNull] IntPtr inProcID);

    /*
        extern OSStatus
        AudioDeviceStop(    AudioObjectID                   inDevice,
                            AudioDeviceIOProcID __nullable  inProcID)                                                   __OSX_AVAILABLE_STARTING(__MAC_10_0, __IPHONE_2_0);
    */

    /// <summary>Stops IO for the given AudioDeviceIOProcID.</summary>
    /// <param name="inDevice">The AudioDevice to stop the IOProc on.</param>
    /// <param name="inProcID">The AudioDeviceIOProcID to stop.</param>
    /// <returns>An OSStatus indicating success or failure.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.0")]
    [LibraryImport(MacLibraries.CoreAudio)]
    public static partial int AudioDeviceStop(AudioObjectID inDevice, [AllowNull] IntPtr inProcID);

    /*!
            @function       AudioDeviceGetCurrentTime
            @abstract       Retrieves the current time from an AudioDevice. Note that the device has to be
                            running.
            @param          inDevice
                                The AudioDevice to from which to get the time.
            @param          outTime
                                An AudioTimeStamp into which the current time is put. On entry, the
                                mFlags field specifies which representations to provide. Because not every
                                device supports all time representations, on exit, the mFlags field will
                                indicate what values are actually valid.
            @result         An OSStatus indicating success or failure. kAudioHardwareNotRunningError will be
                            returned if the AudioDevice isn't running.
        extern OSStatus
        AudioDeviceGetCurrentTime(  AudioObjectID   inDevice,
                                    AudioTimeStamp* outTime)                                                __OSX_AVAILABLE_STARTING(__MAC_10_0, __IPHONE_2_0);
    */
    /// <summary>
    /// Retrieves the current time from an AudioDevice. 
    /// Note that the device has to be running.
    /// </summary>
    /// <param name="inDevice">The AudioDevice to from which to get the time.</param>
    /// <param name="outTime">An AudioTimeStamp into which the current time is put. On entry, the
    /// mFlags field specifies which representations to provide. Because not every
    /// device supports all time representations, on exit, the mFlags field will
    /// indicate what values are actually valid.</param>
    /// <returns>
    /// An OSStatus indicating success or failure.
    /// kAudioHardwareNotRunningError will be returned if the AudioDevice isn't running.
    /// </returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.0")]
    [LibraryImport(MacLibraries.CoreAudio)]
    public static partial int AudioDeviceGetCurrentTime(AudioObjectID inDevice, out AudioTimeStamp outTime);
}