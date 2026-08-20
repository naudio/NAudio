
using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio.Interop;
using System.Diagnostics;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the base type for creating I/O procedures and registering them to the HAL. <br />
/// From this, an input source and an output sink of audio data can be created. <br />
/// This is the type that the <see cref="Wave.CoreAudioPlayer"/> and <see cref="Wave.CoreAudioRecorder"/> classes will use to provide audio data.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public abstract class CoreAudioIOProcedure : SafeHandle
{
    private static readonly unsafe AudioDeviceIOProc procedureInstance = &IOProcedureNative;

    private bool isRunning;
    private GCHandle delegateGcHandle;
    private readonly AudioDevice device;

    // Provide a custom signature for the IO procedure handle,
    // to avoid generics usage. Sure this can be pulled off with
    // generics, but let's make the runtime's life a bit easier.
    private delegate void IOProcedureSignature(
        IntPtr inNow,
        IntPtr inInputData,
        IntPtr inInputTime,
        IntPtr outOutputData,
        IntPtr inOutputTime
    );

    /// <summary>
    /// Allows consumers of the type to initialize the I/O procedure.
    /// </summary>
    /// <param name="dev">The <see cref="AudioDevice"/> to create the I/O procedure for.</param>
    /// <exception cref="ArgumentNullException">The provided audio device was <see langword="null"/>.</exception>
    protected unsafe CoreAudioIOProcedure(AudioDevice dev)
        : base(IntPtr.Zero, true)
    {
        ArgumentNullException.ThrowIfNull(device = dev);
        isRunning = false;
        delegateGcHandle = GCHandle.Alloc(new IOProcedureSignature(IOProcedure), GCHandleType.Normal);
        int osStatus = NativeMethods.AudioDeviceCreateIOProcID(
            dev.objectId,
            procedureInstance,
            GCHandle.ToIntPtr(delegateGcHandle),
            out handle
        );
        if (osStatus != ErrorConstants.kAudioHardwareNoError)
        {
            delegateGcHandle.Free();
            CoreAudioException.ThrowIfError(osStatus);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int IOProcedureNative(
        AudioObjectID inDevice,
        IntPtr inNow,
        IntPtr inInputData,
        IntPtr inInputTime,
        IntPtr outOutputData,
        IntPtr inOutputTime,
        IntPtr inClientData
    )
    {
        try
        {
            ((IOProcedureSignature)GCHandle.FromIntPtr(inClientData).Target)
                .Invoke(
                    inNow,
                    inInputData,
                    inInputTime,
                    outOutputData,
                    inOutputTime
                );
        }
        catch (Exception e)
        {
            Debug.WriteLine("[CoreAudioIOProcedure]: I/O Procedure call threw an exception: " + e);
        }
        return 0;
    }

    /// <summary>
    /// Provides the method that is called automatically by the HAL IO thread as needed. <br />
    /// Data can be retrieved from this, as well as providing data to this.
    /// </summary>
    /// <remarks>
    /// Note that this method must not throw at any circumstance;
    /// if it does so, it will fail fast the process, so try
    /// to catch any exceptions thrown during any exceptional call 
    /// and relay them to your own user code instead.
    /// </remarks>
    /// <seealso href="https://developer.apple.com/documentation/coreaudio/audiodeviceioproc?language=objc"/>
    /// <param name="inNow">
    /// An <see href="https://developer.apple.com/documentation/coreaudiotypes/audiotimestamp?language=objc">AudioTimeStamp</see> pointer that indicates the IO cycle started. 
    /// Note that this time includes any scheduling latency that may have been incurred waking the thread on which IO is being done.
    /// </param>
    /// <param name="inInputData">
    /// An <see href="https://developer.apple.com/documentation/coreaudiotypes/audiobufferlist?language=objc">AudioBufferList</see> pointer containing the input data for the current IO cycle.
    /// For streams that are disabled, the AudioBuffer’s mData field will be NULL but the mDataByteSize field will still say how much data would have been there if it was enabled. 
    /// Note that the contents of this structure should never be modified.
    /// </param>
    /// <param name="inInputTime">
    /// An <see href="https://developer.apple.com/documentation/coreaudiotypes/audiotimestamp?language=objc">AudioTimeStamp</see> pointer that indicates the time at which the first frame in the data was acquired from the hardware. 
    /// If the device has no input streams, the time stamp will be zeroed out.
    /// </param>
    /// <param name="outOutputData">
    /// An <see href="https://developer.apple.com/documentation/coreaudiotypes/audiobufferlist?language=objc">AudioBufferList</see> pointer in which the output data for the current IO cycle is to be placed.
    /// On entry, each AudioBuffer’s mDataByteSize field indicates the maximum amount of data that can be placed in the buffer and the buffer’s memory has been zeroed out. 
    /// For formats where the number of bytes per packet can vary (as with AC-3, for example), the client has to fill out on exit each mDataByteSize field in each AudioBuffer with the amount of data that was put in the buffer. 
    /// Otherwise, the mDataByteSize field should not be changed.
    /// For streams that are disabled, the AudioBuffer’s mData field will be NULL but the mDataByteSize field will still say how much data would have been there if it was enabled.
    /// Except as noted above, the contents of this structure should not other wise be modified.
    /// </param>
    /// <param name="inOutputTime">
    /// An <see href="https://developer.apple.com/documentation/coreaudiotypes/audiotimestamp?language=objc">AudioTimeStamp</see> pointer that indicates the time at which the first frame in the data will be passed to the hardware. 
    /// If the device has no output streams, the time stamp will be zeroed out.
    /// </param>
    protected abstract void IOProcedure(
        IntPtr inNow,
        IntPtr inInputData,
        IntPtr inInputTime,
        IntPtr outOutputData,
        IntPtr inOutputTime
    );

    /// <summary>
    /// Gets the <see cref="AudioDevice"/> that the IO procedure ID was created for.
    /// </summary>
    public AudioDevice Device => device;

    /// <summary>Stops IO.</summary>
    /// <remarks>
    /// This can be called from the IO procedure, 
    /// especially if playback happens and your source of audio data has no more data to provide.
    /// </remarks>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        if (isRunning)
        {
            isRunning = false;
            CoreAudioException.ThrowIfError(NativeMethods.AudioDeviceStop(device.objectId, handle));
        }
    }

    /// <summary>Starts IO.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        if (!isRunning)
        {
            isRunning = true;
            CoreAudioException.ThrowIfError(NativeMethods.AudioDeviceStart(device.objectId, handle));
        }
    }

    /// <summary>
    /// Gets a value whether I/O transactions are performed 
    /// between the <see cref="IOProcedure"/> and the physical audio device.
    /// </summary>
    public bool IsRunning => isRunning;

    /// <summary>
    /// Gets a value whether this IO procedure is invalid.
    /// </summary>
    public sealed override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>
    /// Release the IO procedure and all the required interoperation objects that it needs to communicate over .NET.
    /// </summary>
    /// <returns>A value whether the IO procedure was freed sucessfully.</returns>
    protected sealed override bool ReleaseHandle()
    {
        isRunning = false;
        // Note: The below function invocations may 
        // return bad device error when the device 
        // is disconnected from the HAL.
        // Once this happens, the HAL is seem to dispose everything 
        // associated with the device: It's I/O procedures, 
        // the property listeners, it's controls and everything
        // else associated with it. As such, whence disposing
        // an AudioDevice which was valid at the time of construction
        // but invalid during the time of disposal, could leak the GC handle.
        // As such, observe the bad device error and ignore it
        // when disposing the GC handle. The old check enforced
        // that these invocations pass, and as such could
        // genuinely leak the GC handle if/when the audio
        // device was disconnected from the HAL. 
        // Note that we do still flag to .NET that there is an error
        // when osStatus != 0, but we dispose the delegate GC handle 
        // if there is a bad device error.
        int osStatus = NativeMethods.AudioDeviceStop(device.objectId, handle);
        if (osStatus == ErrorConstants.kAudioHardwareNoError)
        {
            osStatus = NativeMethods.AudioDeviceDestroyIOProcID(device.objectId, handle);
        }
        if (
            (osStatus == 0 || osStatus == ErrorConstants.kAudioHardwareBadDeviceError)
            && delegateGcHandle.IsAllocated
        )
        {
            delegateGcHandle.Free();
        }
        return osStatus == 0;
    }
}