// Shared with NAudio.Windows.Tests, which doesn't enable nullable annotations project-wide.
#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudioAsioFakeDriverTest;

/// <summary>
/// A fake ASIO driver living in this process: a native object whose first field points at a
/// 24-slot IASIO vtable of [UnmanagedCallersOnly] functions. <c>new AsioDriver(FakeAsioDriver.Create())</c>
/// drives NAudio.Asio's binding against it exactly as against a real driver after CoCreateInstance.
/// </summary>
/// <remarks>
/// The native structs are read and written at raw byte offsets taken from the ASIO SDK headers,
/// deliberately not through NAudio's own struct declarations, so a layout mistake in NAudio shows
/// up as a mismatch instead of being mirrored here. The functions use the platform default calling
/// convention, which matches IASIO's thiscall and IUnknown's stdcall on x64 and ARM64 only.
/// State is static, so only one fake exists at a time.
/// </remarks>
internal static unsafe class FakeAsioDriver
{
    public const string DriverName = "NAudio Fake ASIO";
    public const string ErrorMessage = "Fake driver error text";
    public const int DriverVersion = 3;
    public const int InputChannels = 2;
    public const int OutputChannels = 4;
    public const int InputLatency = 64;
    public const int OutputLatency = 128;
    public const int MinBufferSize = 32;
    public const int MaxBufferSize = 2048;
    public const int PreferredBufferSize = 256;
    public const int BufferGranularity = -1;
    public const double InitialSampleRate = 48000;
    public const int ChannelGroup = 7;
    public const int SampleTypeInt32Lsb = 18;   // ASIOSTInt32LSB
    public const uint SamplePositionHi = 1, SamplePositionLo = 2;
    public const uint TimeStampHi = 3, TimeStampLo = 4;

    private const int AseOk = 0;
    private const int AseNotPresent = -1000;
    private const int AseInvalidParameter = -998;
    private const int AseNoClock = -995;

    // Sizes from asio.h.
    public const int ClockSourceSize = 48;   // 4 x long, char name[32]
    public const int ChannelInfoSize = 52;   // long, ASIOBool x2, long, ASIOSampleType, char name[32]
    public static int BufferInfoSize => 8 + 2 * IntPtr.Size;   // ASIOBool, long, void* buffers[2]

    public static double SampleRate;
    public static int InitCalls, ReleaseCalls, StartCalls, StopCalls, OutputReadyCalls;
    public static int DisposeBuffersCalls, ControlPanelCalls, ClockSourceSet;
    public static int CreateBuffersChannels, CreateBuffersSize;
    public static readonly IntPtr[] CallbackPointers = new IntPtr[4];
    public static BufferRecord[] Buffers = [];

    public readonly record struct BufferRecord(bool IsInput, int Channel, IntPtr Buffer0, IntPtr Buffer1);

    private static IntPtr driverObject;
    private static IntPtr vtable;

    public static IntPtr Create()
    {
        Destroy();
        SampleRate = InitialSampleRate;
        InitCalls = ReleaseCalls = StartCalls = StopCalls = OutputReadyCalls = 0;
        DisposeBuffersCalls = ControlPanelCalls = CreateBuffersChannels = CreateBuffersSize = 0;
        ClockSourceSet = -1;
        Array.Clear(CallbackPointers);

        const int slotCount = 24;
        vtable = (IntPtr)NativeMemory.AllocZeroed((nuint)(slotCount * IntPtr.Size));
        var slots = (IntPtr*)vtable;
        slots[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&QueryInterface;
        slots[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&AddRef;
        slots[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&Release;
        slots[3] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&Init;
        slots[4] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, void>)&GetDriverName;
        slots[5] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&GetDriverVersion;
        slots[6] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, void>)&GetErrorMessage;
        slots[7] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&Start;
        slots[8] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&Stop;
        slots[9] = (IntPtr)(delegate* unmanaged<IntPtr, int*, int*, int>)&GetChannels;
        slots[10] = (IntPtr)(delegate* unmanaged<IntPtr, int*, int*, int>)&GetLatencies;
        slots[11] = (IntPtr)(delegate* unmanaged<IntPtr, int*, int*, int*, int*, int>)&GetBufferSize;
        slots[12] = (IntPtr)(delegate* unmanaged<IntPtr, double, int>)&CanSampleRate;
        slots[13] = (IntPtr)(delegate* unmanaged<IntPtr, double*, int>)&GetSampleRate;
        slots[14] = (IntPtr)(delegate* unmanaged<IntPtr, double, int>)&SetSampleRate;
        slots[15] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int*, int>)&GetClockSources;
        slots[16] = (IntPtr)(delegate* unmanaged<IntPtr, int, int>)&SetClockSource;
        slots[17] = (IntPtr)(delegate* unmanaged<IntPtr, uint*, uint*, int>)&GetSamplePosition;
        slots[18] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int>)&GetChannelInfo;
        slots[19] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int, int, IntPtr*, int>)&CreateBuffers;
        slots[20] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&DisposeBuffers;
        slots[21] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&ControlPanel;
        slots[22] = (IntPtr)(delegate* unmanaged<IntPtr, int, IntPtr, int>)&Future;
        slots[23] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&OutputReady;

        driverObject = (IntPtr)NativeMemory.Alloc((nuint)IntPtr.Size);
        *(IntPtr*)driverObject = vtable;
        return driverObject;
    }

    public static void Destroy()
    {
        FreeBuffers();
        if (driverObject != IntPtr.Zero) NativeMemory.Free((void*)driverObject);
        if (vtable != IntPtr.Zero) NativeMemory.Free((void*)vtable);
        driverObject = vtable = IntPtr.Zero;
    }

    // ---- Calling back into the host, as a driver does (ASIOCallbacks are cdecl) ----

    public static void FireBufferSwitch(int doubleBufferIndex, int directProcess) =>
        ((delegate* unmanaged[Cdecl]<int, int, void>)CallbackPointers[0])(doubleBufferIndex, directProcess);

    public static void FireSampleRateDidChange(double sampleRate) =>
        ((delegate* unmanaged[Cdecl]<double, void>)CallbackPointers[1])(sampleRate);

    public static int FireAsioMessage(int selector, int value) =>
        ((delegate* unmanaged[Cdecl]<int, int, IntPtr, IntPtr, int>)CallbackPointers[2])(selector, value, IntPtr.Zero, IntPtr.Zero);

    public static IntPtr FireBufferSwitchTimeInfo(IntPtr asioTime, int doubleBufferIndex, int directProcess) =>
        ((delegate* unmanaged[Cdecl]<IntPtr, int, int, IntPtr>)CallbackPointers[3])(asioTime, doubleBufferIndex, directProcess);

    // ---- IUnknown ----

    [UnmanagedCallersOnly]
    private static int QueryInterface(IntPtr self, Guid* riid, IntPtr* ppv)
    {
        *ppv = IntPtr.Zero;
        return unchecked((int)0x80004002); // E_NOINTERFACE
    }

    [UnmanagedCallersOnly]
    private static uint AddRef(IntPtr self) => 1;

    [UnmanagedCallersOnly]
    private static uint Release(IntPtr self)
    {
        ReleaseCalls++;
        return 0;
    }

    // ---- IASIO ----

    [UnmanagedCallersOnly]
    private static int Init(IntPtr self, IntPtr sysHandle)
    {
        InitCalls++;
        return 1;
    }

    [UnmanagedCallersOnly]
    private static void GetDriverName(IntPtr self, byte* name) => WriteAnsi(name, DriverName, 32);

    [UnmanagedCallersOnly]
    private static int GetDriverVersion(IntPtr self) => DriverVersion;

    [UnmanagedCallersOnly]
    private static void GetErrorMessage(IntPtr self, byte* text) => WriteAnsi(text, ErrorMessage, 124);

    [UnmanagedCallersOnly]
    private static int Start(IntPtr self)
    {
        StartCalls++;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int Stop(IntPtr self)
    {
        StopCalls++;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetChannels(IntPtr self, int* numInputs, int* numOutputs)
    {
        *numInputs = InputChannels;
        *numOutputs = OutputChannels;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetLatencies(IntPtr self, int* input, int* output)
    {
        *input = InputLatency;
        *output = OutputLatency;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetBufferSize(IntPtr self, int* min, int* max, int* preferred, int* granularity)
    {
        *min = MinBufferSize;
        *max = MaxBufferSize;
        *preferred = PreferredBufferSize;
        *granularity = BufferGranularity;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int CanSampleRate(IntPtr self, double sampleRate) =>
        sampleRate is 44100 or 48000 or 96000 ? AseOk : AseNoClock;

    [UnmanagedCallersOnly]
    private static int GetSampleRate(IntPtr self, double* sampleRate)
    {
        *sampleRate = SampleRate;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int SetSampleRate(IntPtr self, double sampleRate)
    {
        SampleRate = sampleRate;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetClockSources(IntPtr self, byte* clocks, int* numSources)
    {
        if (*numSources < 2) return AseInvalidParameter;
        WriteClockSource(clocks, index: 0, associatedChannel: -1, associatedGroup: -1, isCurrent: true, "Internal");
        WriteClockSource(clocks + ClockSourceSize, index: 1, associatedChannel: 2, associatedGroup: 0, isCurrent: false, "Word Clock");
        *numSources = 2;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int SetClockSource(IntPtr self, int reference)
    {
        ClockSourceSet = reference;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetSamplePosition(IntPtr self, uint* samplePosition, uint* timeStamp)
    {
        // ASIOSamples / ASIOTimeStamp: { unsigned long hi; unsigned long lo; }
        samplePosition[0] = SamplePositionHi;
        samplePosition[1] = SamplePositionLo;
        timeStamp[0] = TimeStampHi;
        timeStamp[1] = TimeStampLo;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int GetChannelInfo(IntPtr self, byte* info)
    {
        int channel = *(int*)info;           // in
        bool isInput = *(int*)(info + 4) != 0; // in
        *(int*)(info + 8) = channel == 0 ? 1 : 0;   // isActive
        *(int*)(info + 12) = ChannelGroup;
        *(int*)(info + 16) = SampleTypeInt32Lsb;
        WriteAnsi(info + 20, ChannelName(isInput, channel), 32);
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int CreateBuffers(IntPtr self, byte* bufferInfos, int numChannels, int bufferSize, IntPtr* callbacks)
    {
        FreeBuffers();
        CreateBuffersChannels = numChannels;
        CreateBuffersSize = bufferSize;
        Buffers = new BufferRecord[numChannels];
        for (int i = 0; i < numChannels; i++)
        {
            byte* entry = bufferInfos + i * BufferInfoSize;
            bool isInput = *(int*)entry != 0;
            int channel = *(int*)(entry + 4);
            var buffer0 = (IntPtr)NativeMemory.AllocZeroed((nuint)(bufferSize * 4));
            var buffer1 = (IntPtr)NativeMemory.AllocZeroed((nuint)(bufferSize * 4));
            *(IntPtr*)(entry + 8) = buffer0;
            *(IntPtr*)(entry + 8 + IntPtr.Size) = buffer1;
            Buffers[i] = new BufferRecord(isInput, channel, buffer0, buffer1);
        }
        for (int i = 0; i < CallbackPointers.Length; i++)
        {
            CallbackPointers[i] = callbacks[i];
        }
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int DisposeBuffers(IntPtr self)
    {
        DisposeBuffersCalls++;
        FreeBuffers();
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int ControlPanel(IntPtr self)
    {
        ControlPanelCalls++;
        return AseOk;
    }

    [UnmanagedCallersOnly]
    private static int Future(IntPtr self, int selector, IntPtr opt) => AseNotPresent;

    [UnmanagedCallersOnly]
    private static int OutputReady(IntPtr self)
    {
        OutputReadyCalls++;
        return AseOk;
    }

    // ---- helpers ----

    public static string ChannelName(bool isInput, int channel) => (isInput ? "Fake In " : "Fake Out ") + (channel + 1);

    private static void WriteClockSource(byte* entry, int index, int associatedChannel, int associatedGroup, bool isCurrent, string name)
    {
        *(int*)entry = index;
        *(int*)(entry + 4) = associatedChannel;
        *(int*)(entry + 8) = associatedGroup;
        *(int*)(entry + 12) = isCurrent ? 1 : 0;
        WriteAnsi(entry + 16, name, 32);
    }

    private static void WriteAnsi(byte* destination, string text, int capacity)
    {
        var span = new Span<byte>(destination, capacity);
        span.Clear();
        Encoding.ASCII.GetBytes(text.AsSpan(0, Math.Min(text.Length, capacity - 1)), span);
    }

    private static void FreeBuffers()
    {
        foreach (var buffer in Buffers)
        {
            NativeMemory.Free((void*)buffer.Buffer0);
            NativeMemory.Free((void*)buffer.Buffer1);
        }
        Buffers = [];
    }
}
