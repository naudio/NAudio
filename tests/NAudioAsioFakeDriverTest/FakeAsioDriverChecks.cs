// Shared with NAudio.Windows.Tests, which doesn't enable nullable annotations project-wide.
#nullable enable
using System;
using NAudio.Wave.Asio;

namespace NAudioAsioFakeDriverTest;

/// <summary>
/// End-to-end checks of NAudio.Asio against <see cref="FakeAsioDriver"/>. Shared by the Native AOT
/// console runner (Program.cs) and NAudio.Windows.Tests (JIT), so both runtimes execute the same
/// assertions. Each fake method returns a distinct value, so a vtable slot bound to the wrong
/// index or a mis-laid-out struct shows up as a wrong value rather than passing by coincidence.
/// </summary>
internal static unsafe class FakeAsioDriverChecks
{
    public static int Run(Action<string> log)
    {
        int failures = 0;
        void Check(string name, bool passed, object? actual = null)
        {
            log($"  {(passed ? "PASS" : "FAIL")} {name}{(passed || actual is null ? "" : $" (got: {actual})")}");
            if (!passed) failures++;
        }

        var driverObject = FakeAsioDriver.Create();
        try
        {
            log("Struct layouts:");
            Check("AsioChannelInfoNative is 52 bytes", sizeof(AsioChannelInfoNative) == FakeAsioDriver.ChannelInfoSize, sizeof(AsioChannelInfoNative));
            Check("AsioClockSourceNative is 48 bytes", sizeof(AsioClockSourceNative) == FakeAsioDriver.ClockSourceSize, sizeof(AsioClockSourceNative));
            Check("AsioBufferInfo matches ASIOBufferInfo", sizeof(AsioBufferInfo) == FakeAsioDriver.BufferInfoSize, sizeof(AsioBufferInfo));
            Check("AsioCallbacksNative is four pointers", sizeof(AsioCallbacksNative) == 4 * IntPtr.Size, sizeof(AsioCallbacksNative));

            var driver = new AsioDriver(driverObject);

            log("AsioDriver vtable calls:");
            Check("GetDriverName", driver.GetDriverName() == FakeAsioDriver.DriverName, driver.GetDriverName());
            Check("GetDriverVersion", driver.GetDriverVersion() == FakeAsioDriver.DriverVersion, driver.GetDriverVersion());
            Check("GetErrorMessage", driver.GetErrorMessage() == FakeAsioDriver.ErrorMessage, driver.GetErrorMessage());

            driver.GetChannels(out int inputs, out int outputs);
            Check("GetChannels", inputs == FakeAsioDriver.InputChannels && outputs == FakeAsioDriver.OutputChannels, $"{inputs}/{outputs}");

            var latencyError = driver.GetLatencies(out int inLatency, out int outLatency);
            Check("GetLatencies", latencyError == AsioError.ASE_OK && inLatency == FakeAsioDriver.InputLatency && outLatency == FakeAsioDriver.OutputLatency,
                $"{latencyError} {inLatency}/{outLatency}");

            driver.GetBufferSize(out int min, out int max, out int preferred, out int granularity);
            Check("GetBufferSize",
                min == FakeAsioDriver.MinBufferSize && max == FakeAsioDriver.MaxBufferSize
                && preferred == FakeAsioDriver.PreferredBufferSize && granularity == FakeAsioDriver.BufferGranularity,
                $"{min}/{max}/{preferred}/{granularity}");

            Check("GetSampleRate", driver.GetSampleRate() == FakeAsioDriver.InitialSampleRate, driver.GetSampleRate());
            Check("CanSampleRate(44100)", driver.CanSampleRate(44100));
            Check("CanSampleRate(12345) is false (ASE_NoClock)", !driver.CanSampleRate(12345));
            driver.SetSampleRate(44100);
            Check("SetSampleRate passes the double through", FakeAsioDriver.SampleRate == 44100, FakeAsioDriver.SampleRate);
            driver.SetSampleRate(FakeAsioDriver.InitialSampleRate);

            var clocks = driver.GetClockSources();
            Check("GetClockSources count", clocks.Length == 2, clocks.Length);
            if (clocks.Length == 2)
            {
                Check("Clock source 0", clocks[0] is { Index: 0, AssociatedChannel: -1, AssociatedGroup: -1, IsCurrentSource: 1, Name: "Internal" },
                    $"{clocks[0].Index} {clocks[0].AssociatedChannel} {clocks[0].AssociatedGroup} {clocks[0].IsCurrentSource} '{clocks[0].Name}'");
                Check("Clock source 1", clocks[1] is { Index: 1, AssociatedChannel: 2, AssociatedGroup: 0, IsCurrentSource: 0, Name: "Word Clock" },
                    $"{clocks[1].Index} {clocks[1].AssociatedChannel} {clocks[1].AssociatedGroup} {clocks[1].IsCurrentSource} '{clocks[1].Name}'");
            }
            driver.SetClockSource(1);
            Check("SetClockSource", FakeAsioDriver.ClockSourceSet == 1, FakeAsioDriver.ClockSourceSet);

            driver.GetSamplePosition(out var samplePos, out var timeStamp);
            Check("GetSamplePosition (hi word first)",
                samplePos.ToInt64() == ((long)FakeAsioDriver.SamplePositionHi << 32 | FakeAsioDriver.SamplePositionLo)
                && timeStamp.ToInt64() == ((long)FakeAsioDriver.TimeStampHi << 32 | FakeAsioDriver.TimeStampLo),
                $"{samplePos.ToInt64()}/{timeStamp.ToInt64()}");

            var in2 = driver.GetChannelInfo(1, true);
            Check("GetChannelInfo(1, input)",
                in2.channel == 1 && in2.isInput && !in2.isActive && in2.channelGroup == FakeAsioDriver.ChannelGroup
                && in2.type == AsioSampleType.Int32LSB && in2.name == FakeAsioDriver.ChannelName(true, 1),
                $"{in2.channel} {in2.isInput} {in2.isActive} {in2.channelGroup} {in2.type} '{in2.name}'");
            var out1 = driver.GetChannelInfo(0, false);
            Check("GetChannelInfo(0, output)",
                out1.channel == 0 && !out1.isInput && out1.isActive && out1.name == FakeAsioDriver.ChannelName(false, 0),
                $"{out1.channel} {out1.isInput} {out1.isActive} '{out1.name}'");

            driver.ControlPanel();
            Check("ControlPanel", FakeAsioDriver.ControlPanelCalls == 1, FakeAsioDriver.ControlPanelCalls);

            log("Public CreateBuffers(ref AsioCallbacks) path:");
            int seenIndex = -1;
            bool? seenDirectProcess = null;
            var publicCallbacks = new AsioCallbacks
            {
                pbufferSwitch = (index, directProcess) => { seenIndex = index; seenDirectProcess = directProcess; },
                psampleRateDidChange = _ => { },
                pasioMessage = (_, _, _, _) => 0,
                pbufferSwitchTimeInfo = (asioTime, _, _) => asioTime,
            };
            var oneOutput = new AsioBufferInfo[] { new() { isInput = false, channelNum = 0 } };
            fixed (AsioBufferInfo* pInfos = oneOutput)
            {
                driver.CreateBuffers((IntPtr)pInfos, 1, 64, ref publicCallbacks);
            }
            Check("Driver filled in the buffer pointers",
                oneOutput[0].pBuffer0 != IntPtr.Zero && oneOutput[0].pBuffer0 == FakeAsioDriver.Buffers[0].Buffer0
                && oneOutput[0].pBuffer1 == FakeAsioDriver.Buffers[0].Buffer1);
            FakeAsioDriver.FireBufferSwitch(1, 1);
            Check("bufferSwitch(1, ASIOTrue) arrives as (1, true)", seenIndex == 1 && seenDirectProcess == true, $"{seenIndex} {seenDirectProcess}");
            FakeAsioDriver.FireBufferSwitch(0, 0);
            Check("bufferSwitch(0, ASIOFalse) arrives as (0, false)", seenIndex == 0 && seenDirectProcess == false, $"{seenIndex} {seenDirectProcess}");
            driver.DisposeBuffers();
            driver.DisposeBuffers();
            Check("DisposeBuffers twice is safe", FakeAsioDriver.DisposeBuffersCalls == 2, FakeAsioDriver.DisposeBuffersCalls);
            GC.KeepAlive(publicCallbacks.pbufferSwitch);

            log("AsioDriverExt end to end:");
            var ext = new AsioDriverExt(driver);
            Check("Init called once", FakeAsioDriver.InitCalls == 1, FakeAsioDriver.InitCalls);
            var caps = ext.Capabilities;
            Check("Capabilities: driver name", caps.DriverName == FakeAsioDriver.DriverName, caps.DriverName);
            Check("Capabilities: channel counts",
                caps.NbInputChannels == FakeAsioDriver.InputChannels && caps.NbOutputChannels == FakeAsioDriver.OutputChannels,
                $"{caps.NbInputChannels}/{caps.NbOutputChannels}");
            Check("Capabilities: channel names",
                caps.InputChannelInfos.Length == 2 && caps.InputChannelInfos[1].name == FakeAsioDriver.ChannelName(true, 1)
                && caps.OutputChannelInfos.Length == 4 && caps.OutputChannelInfos[3].name == FakeAsioDriver.ChannelName(false, 3));
            Check("Capabilities: sample rate and latencies",
                caps.SampleRate == FakeAsioDriver.InitialSampleRate
                && caps.InputLatency == FakeAsioDriver.InputLatency && caps.OutputLatency == FakeAsioDriver.OutputLatency);
            Check("Capabilities: buffer sizes",
                caps.BufferMinSize == FakeAsioDriver.MinBufferSize && caps.BufferMaxSize == FakeAsioDriver.MaxBufferSize
                && caps.BufferPreferredSize == FakeAsioDriver.PreferredBufferSize && caps.BufferGranularity == FakeAsioDriver.BufferGranularity);

            int bufferSize = ext.CreateBuffers(2, 1, false);
            Check("CreateBuffers uses the preferred size", bufferSize == FakeAsioDriver.PreferredBufferSize && FakeAsioDriver.CreateBuffersSize == bufferSize,
                $"{bufferSize}/{FakeAsioDriver.CreateBuffersSize}");
            Check("CreateBuffers asks for every channel", FakeAsioDriver.CreateBuffersChannels == FakeAsioDriver.InputChannels + FakeAsioDriver.OutputChannels,
                FakeAsioDriver.CreateBuffersChannels);
            var b = FakeAsioDriver.Buffers;
            Check("Buffer infos: inputs first, then outputs, in channel order",
                b.Length == 6 && b[0] is { IsInput: true, Channel: 0 } && b[1] is { IsInput: true, Channel: 1 }
                && b[2] is { IsInput: false, Channel: 0 } && b[5] is { IsInput: false, Channel: 3 });
            Check("All four callback pointers were passed",
                Array.TrueForAll(FakeAsioDriver.CallbackPointers, p => p != IntPtr.Zero));
            Check("outputReady probed during CreateBuffers", FakeAsioDriver.OutputReadyCalls == 1, FakeAsioDriver.OutputReadyCalls);

            IntPtr[] lastInputs = [], lastOutputs = [];
            ext.FillBufferCallback = (inputBuffers, outputBuffers) =>
            {
                lastInputs = (IntPtr[])inputBuffers.Clone();
                lastOutputs = (IntPtr[])outputBuffers.Clone();
                *(int*)outputBuffers[0] = 0x12345678;
            };
            ext.Start();
            Check("Start", FakeAsioDriver.StartCalls == 1, FakeAsioDriver.StartCalls);

            FakeAsioDriver.FireBufferSwitch(1, 1);
            Check("bufferSwitch(1) hands the callback the second half of each buffer",
                lastInputs.Length == 1 && lastInputs[0] == b[0].Buffer1
                && lastOutputs.Length == 2 && lastOutputs[0] == b[2].Buffer1 && lastOutputs[1] == b[3].Buffer1);
            Check("Samples written by the callback reach the driver's buffer", *(int*)b[2].Buffer1 == 0x12345678, $"0x{*(int*)b[2].Buffer1:X8}");
            Check("outputReady called after the buffer switch", FakeAsioDriver.OutputReadyCalls == 2, FakeAsioDriver.OutputReadyCalls);

            bool gotPosition = ext.TryGetSamplePosition(out long position, out long systemTime);
            Check("TryGetSamplePosition",
                gotPosition && position == ((long)FakeAsioDriver.SamplePositionHi << 32 | FakeAsioDriver.SamplePositionLo)
                && systemTime == ((long)FakeAsioDriver.TimeStampHi << 32 | FakeAsioDriver.TimeStampLo),
                $"{gotPosition} {position} {systemTime}");

            // ASIOTime: reserved[4], then ASIOTimeInfo at 16 (systemTime at 24, samplePosition at 32, flags at 48).
            byte* asioTime = stackalloc byte[148];
            new Span<byte>(asioTime, 148).Clear();
            *(uint*)(asioTime + 24 + 4) = 999;      // systemTime.lo
            *(uint*)(asioTime + 32 + 4) = 4096;     // samplePosition.lo
            *(int*)(asioTime + 48) = 1 | 2;         // kSystemTimeValid | kSamplePositionValid
            var returned = FakeAsioDriver.FireBufferSwitchTimeInfo((IntPtr)asioTime, 0, 1);
            Check("bufferSwitchTimeInfo returns the ASIOTime pointer", returned == (IntPtr)asioTime);
            Check("bufferSwitchTimeInfo(0) hands the callback the first half of each buffer",
                lastInputs.Length == 1 && lastInputs[0] == b[0].Buffer0 && lastOutputs[0] == b[2].Buffer0);
            Check("bufferSwitchTimeInfo timing is read from ASIOTime",
                ext.LatestTimingInfo.SamplePosition == 4096 && ext.LatestTimingInfo.SystemTimeNanoseconds == 999,
                $"{ext.LatestTimingInfo.SamplePosition}/{ext.LatestTimingInfo.SystemTimeNanoseconds}");

            const int selectorSupported = 1, engineVersion = 2, resetRequest = 3, bufferSizeChange = 4;
            Check("asioMessage: kAsioSelectorSupported(kAsioResetRequest) = 1", FakeAsioDriver.FireAsioMessage(selectorSupported, resetRequest) == 1);
            Check("asioMessage: kAsioSelectorSupported(unknown) = 0", FakeAsioDriver.FireAsioMessage(selectorSupported, 0x7fff) == 0);
            Check("asioMessage: kAsioEngineVersion = 2", FakeAsioDriver.FireAsioMessage(engineVersion, 0) == 2);
            bool resetRequested = false;
            ext.ResetRequestCallback = () => resetRequested = true;
            Check("asioMessage: kAsioResetRequest raises ResetRequestCallback",
                FakeAsioDriver.FireAsioMessage(resetRequest, 0) == 1 && resetRequested);
            int newBufferSize = 0;
            ext.BufferSizeChangedCallback = size => newBufferSize = size;
            FakeAsioDriver.FireAsioMessage(bufferSizeChange, 512);
            Check("asioMessage: kAsioBufferSizeChange passes the new size", newBufferSize == 512, newBufferSize);

            FakeAsioDriver.FireSampleRateDidChange(96000);
            Check("sampleRateDidChange updates the capabilities", ext.Capabilities.SampleRate == 96000, ext.Capabilities.SampleRate);

            ext.Stop();
            Check("Stop", FakeAsioDriver.StopCalls == 1, FakeAsioDriver.StopCalls);
            ext.DisposeBuffers();
            ext.ReleaseDriver();
            Check("DisposeBuffers then ReleaseDriver (disposes again, then releases)",
                FakeAsioDriver.DisposeBuffersCalls == 4 && FakeAsioDriver.ReleaseCalls == 1,
                $"{FakeAsioDriver.DisposeBuffersCalls} disposes, {FakeAsioDriver.ReleaseCalls} releases");
        }
        catch (Exception ex)
        {
            log($"  FAIL unexpected exception: {ex}");
            failures++;
        }
        finally
        {
            FakeAsioDriver.Destroy();
        }
        return failures;
    }
}
