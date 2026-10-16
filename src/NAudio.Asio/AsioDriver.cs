using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NAudio.Wave.Asio;

/// <summary>
/// Main AsioDriver Class. To use this class, you need to query first the GetAsioDriverNames() and
/// then use the GetAsioDriverByName to instantiate the correct AsioDriver.
/// This is the first AsioDriver binding fully implemented in C#!
/// 
/// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
/// </summary>
public unsafe class AsioDriver
{
    private IntPtr pAsioComObject;
    private IntPtr pinnedcallbacks;
    private AsioDriverVTable asioDriverVTable;

    // ASIO caps names at 32 characters and error messages at 124; 256 leaves ample headroom.
    private const int StringBufferSize = 256;

    private AsioDriver(Guid guid)
    {
        InitFromGuid(guid);
    }

    /// <summary>
    /// Wraps an already-created ASIO driver object. Used by tests to drive the binding against a
    /// fake in-process driver, without going through the registry and <c>CoCreateInstance</c>.
    /// </summary>
    /// <param name="asioComObject">Pointer to the driver object, whose first field is its vtable pointer.</param>
    internal AsioDriver(IntPtr asioComObject)
    {
        pAsioComObject = asioComObject;
        asioDriverVTable = new AsioDriverVTable(asioComObject);
    }

    /// <summary>
    /// Gets the ASIO driver names installed.
    /// </summary>
    /// <returns>a list of driver names. Use this name to GetAsioDriverByName</returns>
    public static string[] GetAsioDriverNames()
    {
        var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
        var names = new string[0];
        if (regKey != null)
        {
            names = regKey.GetSubKeyNames();
            regKey.Close();
        }
        return names;
    }

    /// <summary>
    /// Instantiate a AsioDriver given its name.
    /// </summary>
    /// <param name="name">The name of the driver</param>
    /// <returns>an AsioDriver instance</returns>
    public static AsioDriver GetAsioDriverByName(String name)
    {
        var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO\\" + name);
        if (regKey == null)
        {
            throw new ArgumentException($"Driver Name {name} doesn't exist");
        }
        var guid = regKey.GetValue("CLSID")?.ToString()
            ?? throw new ArgumentException($"Driver Name {name} has no CLSID registered");
        return GetAsioDriverByGuid(new Guid(guid));
    }

    /// <summary>
    /// Instantiate the ASIO driver by GUID.
    /// </summary>
    /// <param name="guid">The GUID.</param>
    /// <returns>an AsioDriver instance</returns>
    public static AsioDriver GetAsioDriverByGuid(Guid guid)
    {
        return new AsioDriver(guid);
    }

    /// <summary>
    /// Inits the AsioDriver..
    /// </summary>
    /// <param name="sysHandle">The sys handle.</param>
    /// <returns></returns>
    public bool Init(IntPtr sysHandle)
    {
        int ret = asioDriverVTable.Init(pAsioComObject, sysHandle);
        return ret == 1;
    }

    /// <summary>
    /// Gets the name of the driver.
    /// </summary>
    /// <returns></returns>
    public string GetDriverName()
    {
        byte* name = stackalloc byte[StringBufferSize];
        new Span<byte>(name, StringBufferSize).Clear();
        asioDriverVTable.GetDriverName(pAsioComObject, name);
        return AsioNativeStrings.Read(name, StringBufferSize);
    }

    /// <summary>
    /// Gets the driver version.
    /// </summary>
    /// <returns></returns>
    public int GetDriverVersion()
    {
        return asioDriverVTable.GetDriverVersion(pAsioComObject);
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <returns></returns>
    public string GetErrorMessage()
    {
        byte* errorMessage = stackalloc byte[StringBufferSize];
        new Span<byte>(errorMessage, StringBufferSize).Clear();
        asioDriverVTable.GetErrorMessage(pAsioComObject, errorMessage);
        return AsioNativeStrings.Read(errorMessage, StringBufferSize);
    }

    /// <summary>
    /// Starts this instance.
    /// </summary>
    public void Start()
    {
        HandleException(asioDriverVTable.Start(pAsioComObject), "start");
    }

    /// <summary>
    /// Stops this instance.
    /// </summary>
    public AsioError Stop()
    {
        return asioDriverVTable.Stop(pAsioComObject);
    }

    /// <summary>
    /// Gets the number of channels.
    /// </summary>
    /// <param name="numInputChannels">The num input channels.</param>
    /// <param name="numOutputChannels">The num output channels.</param>
    public void GetChannels(out int numInputChannels, out int numOutputChannels)
    {
        int inputs = 0, outputs = 0;
        var error = asioDriverVTable.GetChannels(pAsioComObject, &inputs, &outputs);
        numInputChannels = inputs;
        numOutputChannels = outputs;
        HandleException(error, "getChannels");
    }

    /// <summary>
    /// Gets the latencies (n.b. does not throw an exception)
    /// </summary>
    /// <param name="inputLatency">The input latency.</param>
    /// <param name="outputLatency">The output latency.</param>
    public AsioError GetLatencies(out int inputLatency, out int outputLatency)
    {
        int input = 0, output = 0;
        var error = asioDriverVTable.GetLatencies(pAsioComObject, &input, &output);
        inputLatency = input;
        outputLatency = output;
        return error;
    }

    /// <summary>
    /// Gets the size of the buffer.
    /// </summary>
    /// <param name="minSize">Size of the min.</param>
    /// <param name="maxSize">Size of the max.</param>
    /// <param name="preferredSize">Size of the preferred.</param>
    /// <param name="granularity">The granularity.</param>
    public void GetBufferSize(out int minSize, out int maxSize, out int preferredSize, out int granularity)
    {
        int min = 0, max = 0, preferred = 0, gran = 0;
        var error = asioDriverVTable.GetBufferSize(pAsioComObject, &min, &max, &preferred, &gran);
        minSize = min;
        maxSize = max;
        preferredSize = preferred;
        granularity = gran;
        HandleException(error, "getBufferSize");
    }

    /// <summary>
    /// Determines whether this instance can use the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate.</param>
    /// <returns>
    /// 	<c>true</c> if this instance [can sample rate] the specified sample rate; otherwise, <c>false</c>.
    /// </returns>
    public bool CanSampleRate(double sampleRate)
    {
        var error = asioDriverVTable.CanSampleRate(pAsioComObject, sampleRate);
        if (error == AsioError.ASE_NoClock)
        {
            return false;
        }
        if (error == AsioError.ASE_OK)
        {
            return true;
        }
        HandleException(error, "canSampleRate");
        return false;
    }

    /// <summary>
    /// Gets the sample rate.
    /// </summary>
    /// <returns></returns>
    public double GetSampleRate()
    {
        double sampleRate = 0;
        HandleException(asioDriverVTable.GetSampleRate(pAsioComObject, &sampleRate), "getSampleRate");
        return sampleRate;
    }

    /// <summary>
    /// Sets the sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate.</param>
    public void SetSampleRate(double sampleRate)
    {
        HandleException(asioDriverVTable.SetSampleRate(pAsioComObject, sampleRate), "setSampleRate");
    }

    /// <summary>
    /// Enumerates the clock sources available to the driver (Internal, Word Clock, S/PDIF, ADAT, …).
    /// Each entry's <see cref="AsioClockSource.IsCurrentSource"/> indicates whether the driver is
    /// currently locked to it; the <see cref="AsioClockSource.Index"/> is the value to pass to
    /// <see cref="SetClockSource(int)"/>. The driver may signal <c>kAsioResetRequest</c> in response
    /// to a clock-source change, which surfaces via <c>AsioDevice.DriverResetRequest</c>.
    /// </summary>
    /// <param name="maxSources">Upper bound on the number of sources to retrieve. 16 is generous —
    /// even high-end interfaces rarely report more than 8.</param>
    public AsioClockSource[] GetClockSources(int maxSources = 16)
    {
        if (maxSources <= 0) throw new ArgumentOutOfRangeException(nameof(maxSources));
        // The public AsioClockSource carries a [ByValTStr] string, so the driver writes into a
        // blittable mirror and each entry is converted afterwards (no runtime marshalling).
        var native = new AsioClockSourceNative[maxSources];
        int numSources = maxSources;
        fixed (AsioClockSourceNative* pNative = native)
        {
            HandleException(
                asioDriverVTable.GetClockSources(pAsioComObject, pNative, &numSources),
                "getClockSources");
        }
        if (numSources < 0) numSources = 0;
        if (numSources > maxSources) numSources = maxSources;
        var result = new AsioClockSource[numSources];
        for (int i = 0; i < numSources; i++)
        {
            result[i] = native[i].ToManaged();
        }
        return result;
    }

    /// <summary>
    /// Sets the clock source. Use one of the <see cref="AsioClockSource.Index"/> values returned
    /// by <see cref="GetClockSources"/>.
    /// </summary>
    /// <param name="reference">Clock source index reported by the driver.</param>
    public void SetClockSource(int reference)
    {
        HandleException(asioDriverVTable.SetClockSource(pAsioComObject, reference), "setClockSource");
    }

    /// <summary>
    /// Gets the current sample position and the host system time (in nanoseconds) at which it was sampled.
    /// </summary>
    /// <param name="samplePos">Sample position since <c>Start()</c>, as a 64-bit ASIO value.</param>
    /// <param name="timeStamp">System time in nanoseconds corresponding to <paramref name="samplePos"/>.</param>
    public void GetSamplePosition(out Asio64Bit samplePos, out Asio64Bit timeStamp)
    {
        HandleException(TryGetSamplePosition(out samplePos, out timeStamp), "getSamplePosition");
    }

    /// <summary>
    /// Non-throwing variant of <see cref="GetSamplePosition"/>, safe to call from the realtime
    /// buffer-switch callback. Returns the raw <see cref="AsioError"/> instead of throwing.
    /// </summary>
    internal AsioError TryGetSamplePosition(out Asio64Bit samplePos, out Asio64Bit timeStamp)
    {
        Asio64Bit pos = default, stamp = default;
        var error = asioDriverVTable.GetSamplePosition(pAsioComObject, &pos, &stamp);
        samplePos = pos;
        timeStamp = stamp;
        return error;
    }

    /// <summary>
    /// Gets the channel info.
    /// </summary>
    /// <param name="channelNumber">The channel number.</param>
    /// <param name="trueForInputInfo">if set to <c>true</c> [true for input info].</param>
    /// <returns>Channel Info</returns>
    public AsioChannelInfo GetChannelInfo(int channelNumber, bool trueForInputInfo)
    {
        var info = new AsioChannelInfoNative { channel = channelNumber, isInput = trueForInputInfo ? 1 : 0 };
        HandleException(asioDriverVTable.GetChannelInfo(pAsioComObject, &info), "getChannelInfo");
        return info.ToManaged();
    }

    /// <summary>
    /// Creates the buffers.
    /// </summary>
    /// <param name="bufferInfos">The buffer infos.</param>
    /// <param name="numChannels">The num channels.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    /// <param name="callbacks">The callbacks.</param>
    /// <remarks>The caller must keep the delegates in <paramref name="callbacks"/> alive until
    /// <see cref="DisposeBuffers"/> is called, because the driver holds pointers to them.</remarks>
    public void CreateBuffers(IntPtr bufferInfos, int numChannels, int bufferSize, ref AsioCallbacks callbacks)
    {
        var native = new AsioCallbacksNative
        {
            bufferSwitch = callbacks.pbufferSwitch is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(callbacks.pbufferSwitch),
            sampleRateDidChange = callbacks.psampleRateDidChange is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(callbacks.psampleRateDidChange),
            asioMessage = callbacks.pasioMessage is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(callbacks.pasioMessage),
            bufferSwitchTimeInfo = callbacks.pbufferSwitchTimeInfo is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(callbacks.pbufferSwitchTimeInfo),
        };
        CreateBuffers(bufferInfos, numChannels, bufferSize, in native);
    }

    /// <summary>
    /// Creates the buffers, passing the driver a callback table of raw function pointers. The
    /// caller owns the delegates behind those pointers and must keep them alive until
    /// <see cref="DisposeBuffers"/>.
    /// </summary>
    internal void CreateBuffers(IntPtr bufferInfos, int numChannels, int bufferSize, in AsioCallbacksNative callbacks)
    {
        // The driver keeps the callbacks pointer until disposeBuffers, so it must not move.
        pinnedcallbacks = Marshal.AllocHGlobal(sizeof(AsioCallbacksNative));
        *(AsioCallbacksNative*)pinnedcallbacks = callbacks;
        HandleException(asioDriverVTable.CreateBuffers(pAsioComObject, bufferInfos, numChannels, bufferSize, pinnedcallbacks), "createBuffers");
    }

    /// <summary>
    /// Disposes the buffers.
    /// </summary>
    public AsioError DisposeBuffers()
    {
        AsioError result = asioDriverVTable.DisposeBuffers(pAsioComObject);
        // Clear the pointer so a second DisposeBuffers (e.g. ReleaseDriver after a failed
        // re-initialisation) doesn't free the same block twice.
        Marshal.FreeHGlobal(pinnedcallbacks);
        pinnedcallbacks = IntPtr.Zero;
        return result;
    }

    /// <summary>
    /// Controls the panel.
    /// </summary>
    public void ControlPanel()
    {
        HandleException(asioDriverVTable.ControlPanel(pAsioComObject), "controlPanel");
    }

    /// <summary>
    /// Futures the specified selector.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="opt">The opt.</param>
    public void Future(int selector, IntPtr opt)
    {
        HandleException(asioDriverVTable.Future(pAsioComObject, selector, opt), "future");
    }

    /// <summary>
    /// Notifies OutputReady to the AsioDriver.
    /// </summary>
    /// <returns></returns>
    public AsioError OutputReady()
    {
        return asioDriverVTable.OutputReady(pAsioComObject);
    }

    /// <summary>
    /// Releases this instance.
    /// </summary>
    public void ReleaseComAsioDriver()
    {
        asioDriverVTable.Release(pAsioComObject);
    }

    /// <summary>
    /// Handles the exception. Throws an exception based on the error.
    /// </summary>
    /// <param name="error">The error to check.</param>
    /// <param name="methodName">Method name</param>
    private void HandleException(AsioError error, string methodName)
    {
        if (error != AsioError.ASE_OK && error != AsioError.ASE_SUCCESS)
        {
            var asioException = new AsioException(
                $"Error code [{AsioException.getErrorName(error)}] while calling ASIO method <{methodName}>, {this.GetErrorMessage()}");
            asioException.Error = error;
            throw asioException;
        }
    }

    /// <summary>
    /// Inits the vTable method from GUID. This is a tricky part of this class.
    /// </summary>
    /// <param name="asioGuid">The ASIO GUID.</param>
    private void InitFromGuid(Guid asioGuid)
    {
        const uint CLSCTX_INPROC_SERVER = 1;

        // Pointer to the ASIO object
        // USE CoCreateInstance instead of builtin COM-Class instantiation,
        // because the AsioDriver expect to have the ASIOGuid used for both COM Object and COM interface
        // The CoCreateInstance is working only in STAThread mode.
        int hresult = CoCreateInstance(ref asioGuid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref asioGuid, out pAsioComObject);
        if (hresult != 0)
        {
            throw new InvalidOperationException($"Unable to instantiate ASIO. Check if STAThread is set (HRESULT: 0x{hresult:X8})");
        }

        asioDriverVTable = new AsioDriverVTable(pAsioComObject);
    }

    /// <summary>
    /// Typed function pointers for the IASIO C++ vtable. Each slot is bound explicitly by index,
    /// with blittable parameters only, so calls involve no reflection and no runtime marshalling
    /// and behave identically under the JIT and Native AOT. IASIO methods are C++ virtuals with
    /// the default (thiscall) member convention; Release is the COM IUnknown method (stdcall).
    /// Thiscall and stdcall are the same as the default convention on x64 and ARM64.
    /// </summary>
    private readonly struct AsioDriverVTable
    {
        public readonly delegate* unmanaged[Stdcall]<IntPtr, uint> Release;                                       // 2  IUnknown::Release
        public readonly delegate* unmanaged[Thiscall]<IntPtr, IntPtr, int> Init;                                  // 3  ASIOBool init(void *sysHandle)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, byte*, void> GetDriverName;                         // 4  void getDriverName(char *name)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int> GetDriverVersion;                              // 5  long getDriverVersion()
        public readonly delegate* unmanaged[Thiscall]<IntPtr, byte*, void> GetErrorMessage;                       // 6  void getErrorMessage(char *string)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioError> Start;                                   // 7  ASIOError start()
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioError> Stop;                                    // 8  ASIOError stop()
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int*, int*, AsioError> GetChannels;                 // 9  getChannels(long *numInputChannels, long *numOutputChannels)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int*, int*, AsioError> GetLatencies;                // 10 getLatencies(long *inputLatency, long *outputLatency)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int*, int*, int*, int*, AsioError> GetBufferSize;   // 11 getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, double, AsioError> CanSampleRate;                   // 12 canSampleRate(ASIOSampleRate sampleRate)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, double*, AsioError> GetSampleRate;                  // 13 getSampleRate(ASIOSampleRate *sampleRate)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, double, AsioError> SetSampleRate;                   // 14 setSampleRate(ASIOSampleRate sampleRate)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioClockSourceNative*, int*, AsioError> GetClockSources; // 15 getClockSources(ASIOClockSource *clocks, long *numSources)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int, AsioError> SetClockSource;                     // 16 setClockSource(long reference)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, Asio64Bit*, Asio64Bit*, AsioError> GetSamplePosition; // 17 getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioChannelInfoNative*, AsioError> GetChannelInfo;  // 18 getChannelInfo(ASIOChannelInfo *info)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, IntPtr, int, int, IntPtr, AsioError> CreateBuffers; // 19 createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioError> DisposeBuffers;                          // 20 disposeBuffers()
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioError> ControlPanel;                            // 21 controlPanel()
        public readonly delegate* unmanaged[Thiscall]<IntPtr, int, IntPtr, AsioError> Future;                     // 22 future(long selector, void *opt)
        public readonly delegate* unmanaged[Thiscall]<IntPtr, AsioError> OutputReady;                             // 23 outputReady()

        public AsioDriverVTable(IntPtr asioComObject)
        {
            // The first field of the driver object is a pointer to its C++ vtable.
            var slots = *(IntPtr**)asioComObject;
            Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)slots[2];
            Init = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, int>)slots[3];
            GetDriverName = (delegate* unmanaged[Thiscall]<IntPtr, byte*, void>)slots[4];
            GetDriverVersion = (delegate* unmanaged[Thiscall]<IntPtr, int>)slots[5];
            GetErrorMessage = (delegate* unmanaged[Thiscall]<IntPtr, byte*, void>)slots[6];
            Start = (delegate* unmanaged[Thiscall]<IntPtr, AsioError>)slots[7];
            Stop = (delegate* unmanaged[Thiscall]<IntPtr, AsioError>)slots[8];
            GetChannels = (delegate* unmanaged[Thiscall]<IntPtr, int*, int*, AsioError>)slots[9];
            GetLatencies = (delegate* unmanaged[Thiscall]<IntPtr, int*, int*, AsioError>)slots[10];
            GetBufferSize = (delegate* unmanaged[Thiscall]<IntPtr, int*, int*, int*, int*, AsioError>)slots[11];
            CanSampleRate = (delegate* unmanaged[Thiscall]<IntPtr, double, AsioError>)slots[12];
            GetSampleRate = (delegate* unmanaged[Thiscall]<IntPtr, double*, AsioError>)slots[13];
            SetSampleRate = (delegate* unmanaged[Thiscall]<IntPtr, double, AsioError>)slots[14];
            GetClockSources = (delegate* unmanaged[Thiscall]<IntPtr, AsioClockSourceNative*, int*, AsioError>)slots[15];
            SetClockSource = (delegate* unmanaged[Thiscall]<IntPtr, int, AsioError>)slots[16];
            GetSamplePosition = (delegate* unmanaged[Thiscall]<IntPtr, Asio64Bit*, Asio64Bit*, AsioError>)slots[17];
            GetChannelInfo = (delegate* unmanaged[Thiscall]<IntPtr, AsioChannelInfoNative*, AsioError>)slots[18];
            CreateBuffers = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, int, int, IntPtr, AsioError>)slots[19];
            DisposeBuffers = (delegate* unmanaged[Thiscall]<IntPtr, AsioError>)slots[20];
            ControlPanel = (delegate* unmanaged[Thiscall]<IntPtr, AsioError>)slots[21];
            Future = (delegate* unmanaged[Thiscall]<IntPtr, int, IntPtr, AsioError>)slots[22];
            OutputReady = (delegate* unmanaged[Thiscall]<IntPtr, AsioError>)slots[23];
        }
    }

    [DllImport("ole32.Dll")]
    private static extern int CoCreateInstance(ref Guid clsid,
       IntPtr inner,
       uint context,
       ref Guid uuid,
       out IntPtr rReturnedComObject);
}
