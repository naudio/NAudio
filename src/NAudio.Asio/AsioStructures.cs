using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio;

// -------------------------------------------------------------------------------
// Structures used by the AsioDriver and ASIODriverExt
// -------------------------------------------------------------------------------

// -------------------------------------------------------------------------------
// Error and Exceptions
// -------------------------------------------------------------------------------

/// <summary>
/// ASIO common Exception.
/// </summary>
internal class AsioException : Exception
{
    private AsioError error;

    public AsioException()
    {
    }

    public AsioException(string message)
        : base(message)
    {
    }

    public AsioException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AsioError Error
    {
        get { return error; }
        set
        {
            error = value;
            Data["ASIOError"] = error;
        }
    }

    /// <summary>
    /// Gets the name of the error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>the name of the error</returns>
    public static String? getErrorName(AsioError error)
    {
        return Enum.GetName(typeof(AsioError), error);
    }
}

// -------------------------------------------------------------------------------
// Channel Info, Buffer Info
// -------------------------------------------------------------------------------


/// <summary>
/// One available clock source as reported by <c>ASIOGetClockSources</c>. Pro audio interfaces
/// typically expose Internal alongside Word Clock, S/PDIF, AES/EBU, and ADAT sync inputs;
/// consumer interfaces usually report a single Internal source.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct AsioClockSource
{
    /// <summary>Reference index passed to <c>SetClockSource</c>.</summary>
    public int Index;
    /// <summary>Associated input channel (e.g. the S/PDIF input), or -1 if not channel-bound.</summary>
    public int AssociatedChannel;
    /// <summary>Associated channel group, or -1.</summary>
    public int AssociatedGroup;
    /// <summary>Non-zero if this is the source the driver is currently locked to.</summary>
    public int IsCurrentSource;
    /// <summary>Human-readable name for the source (e.g. "Internal", "Word Clock", "S/PDIF").</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string Name;
}

/// <summary>
/// Blittable mirror of the native <c>ASIOClockSource</c> (48 bytes). The driver writes into this
/// directly; <see cref="ToManaged"/> converts it to the public <see cref="AsioClockSource"/>, whose
/// <c>[ByValTStr]</c> name would otherwise need runtime marshalling.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct AsioClockSourceNative
{
    public const int NameLength = 32;

    public int index;
    public int associatedChannel;
    public int associatedGroup;
    public int isCurrentSource;   // ASIOBool
    public fixed byte name[NameLength];

    public AsioClockSource ToManaged()
    {
        fixed (byte* pName = name)
        {
            return new AsioClockSource
            {
                Index = index,
                AssociatedChannel = associatedChannel,
                AssociatedGroup = associatedGroup,
                IsCurrentSource = isCurrentSource,
                Name = AsioNativeStrings.Read(pName, NameLength),
            };
        }
    }
}

/// <summary>
/// Blittable mirror of the native <c>ASIOChannelInfo</c> (52 bytes). ASIOBool and ASIOSampleType
/// are both 32-bit in the SDK, so they're plain ints here.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct AsioChannelInfoNative
{
    public const int NameLength = 32;

    public int channel;           // on input, channel index
    public int isInput;           // on input, ASIOBool
    public int isActive;          // on exit, ASIOBool
    public int channelGroup;      // on exit
    public int type;              // on exit, ASIOSampleType
    public fixed byte name[NameLength];

    public AsioChannelInfo ToManaged()
    {
        fixed (byte* pName = name)
        {
            return new AsioChannelInfo
            {
                channel = channel,
                isInput = isInput != 0,
                isActive = isActive != 0,
                channelGroup = channelGroup,
                type = (AsioSampleType)type,
                name = AsioNativeStrings.Read(pName, NameLength),
            };
        }
    }
}

/// <summary>
/// Native <c>ASIOCallbacks</c>: four C function pointers that the driver keeps from
/// <c>createBuffers</c> until <c>disposeBuffers</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AsioCallbacksNative
{
    public IntPtr bufferSwitch;          // void (*)(long doubleBufferIndex, ASIOBool directProcess)
    public IntPtr sampleRateDidChange;   // void (*)(ASIOSampleRate sRate)
    public IntPtr asioMessage;           // long (*)(long selector, long value, void* message, double* opt)
    public IntPtr bufferSwitchTimeInfo;  // ASIOTime* (*)(ASIOTime* params, long doubleBufferIndex, ASIOBool directProcess)

    // Blittable delegate signatures for the callbacks (ASIOBool and long are 32-bit ints), so the
    // reverse-P/Invoke thunks need no marshalling under the JIT or Native AOT.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BufferSwitch(int doubleBufferIndex, int directProcess);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SampleRateDidChange(double sRate);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int AsioMessage(int selector, int value, IntPtr message, IntPtr opt);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr BufferSwitchTimeInfo(IntPtr asioTimeParam, int doubleBufferIndex, int directProcess);
}

internal static unsafe class AsioNativeStrings
{
    /// <summary>
    /// Reads a fixed-size, normally null-terminated ANSI string field, stopping at the field
    /// size if the driver didn't terminate it.
    /// </summary>
    public static string Read(byte* field, int size)
    {
        int length = new ReadOnlySpan<byte>(field, size).IndexOf((byte)0);
        if (length < 0) length = size;
        return Marshal.PtrToStringAnsi((IntPtr)field, length);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct AsioBufferInfo
{
    public bool isInput;       // on input:  ASIOTrue: input, else output
    public int channelNum;     // on input:  channel index
    public IntPtr pBuffer0;    // on output: double buffer addresses
    public IntPtr pBuffer1;    // on output: double buffer addresses

    public IntPtr Buffer(int bufferIndex)
    {
        return (bufferIndex == 0) ? pBuffer0 : pBuffer1;
    }

}

// -------------------------------------------------------------------------------
// Time structures
// -------------------------------------------------------------------------------

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
internal struct AsioTimeCode
{
    public double speed;                  // speed relation (fraction of nominal speed)
    // ASIOSamples     timeCodeSamples;        // time in samples
    public Asio64Bit timeCodeSamples;        // time in samples
    public AsioTimeCodeFlags flags;                  // some information flags (see below)
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string future;
}

[Flags]
internal enum AsioTimeCodeFlags
{
    kTcValid = 1,
    kTcRunning = 1 << 1,
    kTcReverse = 1 << 2,
    kTcOnspeed = 1 << 3,
    kTcStill = 1 << 4,
    kTcSpeedValid = 1 << 8
};

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
internal struct AsioTimeInfo
{
    public double speed;                  // absolute speed (1. = nominal)
    public Asio64Bit systemTime;             // system time related to samplePosition, in nanoseconds
    public Asio64Bit samplePosition;
    public double sampleRate;             // current rate
    public AsioTimeInfoFlags flags;                    // (see below)
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
    public string reserved;
}

[Flags]
internal enum AsioTimeInfoFlags
{
    kSystemTimeValid = 1,            // must always be valid
    kSamplePositionValid = 1 << 1,       // must always be valid
    kSampleRateValid = 1 << 2,
    kSpeedValid = 1 << 3,
    kSampleRateChanged = 1 << 4,
    kClockSourceChanged = 1 << 5
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
internal struct AsioTime
{                         // both input/output
    public int reserved1;
    public int reserved2;
    public int reserved3;
    public int reserved4;
    public AsioTimeInfo timeInfo;       // required
    public AsioTimeCode timeCode;       // optional, evaluated if (timeCode.flags & kTcValid)
}
