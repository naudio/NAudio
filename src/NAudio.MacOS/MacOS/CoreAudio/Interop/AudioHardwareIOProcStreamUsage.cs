// This interop definition was derived from the file AudioHardware.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// This structure describes which streams a given AudioDeviceIOProc will use. It is
/// used in conjunction with <see cref="AudioDeviceProperties.kAudioDevicePropertyIOProcStreamUsage"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AudioHardwareIOProcStreamUsage
{
    /// <summary>
    /// The IOProc whose stream usage is being specified.
    /// </summary>
    public AudioDeviceIOProc mIOProc;
    /// <summary>
    /// The number of streams being specified.
    /// </summary>
    public uint mNumberStreams;
    /// <summary>
    /// An array of UInt32's whose length is specified by mNumberStreams. Each
    /// element of the array corresponds to a stream. A value of 0 means the stream
    /// is not to be enabled. Any other value means the stream is to be used.
    /// </summary>
    public fixed uint mStreamIsOn[1];

    public static IntPtr CreateStreamUsage(AudioDeviceIOProc ioProc, uint nStreams, out uint size)
    {
        size = (uint)sizeof(AudioHardwareIOProcStreamUsage);
        // Verify that nStreams is greater than zero;
        // if zero, we just create a memory block
        // with the size of this structure.
        if (nStreams > 0U)
        {
            size += ((nStreams - 1U) * sizeof(uint));
        }
        var usage = (AudioHardwareIOProcStreamUsage*)NativeMemory.Alloc(size);
        usage->mIOProc = ioProc;
        usage->mNumberStreams = nStreams;
        return new(usage);
    }

    public static IntPtr CreateStreamUsage(AudioDeviceIOProc ioProc, uint sizeInBytes)
    {
        var usage = (AudioHardwareIOProcStreamUsage*)NativeMemory.Alloc(sizeInBytes);
        usage->mIOProc = ioProc;
        return new(usage);
    }

    public static void DeleteStreamUsage(IntPtr memBlock) => NativeMemory.Free(memBlock.ToPointer());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetStream(IntPtr memBlock, uint streamNumber, bool enable)
        => ((AudioHardwareIOProcStreamUsage*)memBlock.ToPointer())->mStreamIsOn[streamNumber] = enable ? 1U : 0U;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetStream(IntPtr memBlock, uint streamNumber)
        => ((AudioHardwareIOProcStreamUsage*)memBlock.ToPointer())->mStreamIsOn[streamNumber] != 0U;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetNumberOfStreams(IntPtr memBlock) => ((AudioHardwareIOProcStreamUsage*)memBlock.ToPointer())->mNumberStreams;

}