using System.Diagnostics;
using NAudio.Midi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM.Tests;

/// <summary>
/// Sends sysex buffers through <see cref="MidiOut.SendBuffer"/> and reopens the device, to
/// exercise the long-message buffer and handle lifetime fixed in #1478. Only silent messages
/// are sent (GM System On, a non-commercial sysex receivers ignore, All Notes Off), so it is
/// safe to point at the Microsoft GS Wavetable Synth when no hardware MIDI port is attached.
/// </summary>
internal sealed class WinMmMidiOutSysexTest : IConsoleTest
{
    // Universal Non-Real Time: GM System On. A synth resets to GM defaults; no sound.
    private static readonly byte[] GmSystemOn = [0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7];

    public string Id => "WinMm.MidiOutSysex";
    public string Description => "Send sysex via MidiOut.SendBuffer and reopen the device (#1478 regression)";
    public MenuPath? MenuLocation =>
        new("WinMM (Windows Multimedia)", "MIDI out sysex", Group: "MIDI", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("deviceNumber", typeof(int), Required: false, Default: 0, Help: "MidiOut device index"),
        new("iterations", typeof(int), Required: false, Default: 20, Help: "number of sysex sends per open"),
        new("largeSysexBytes", typeof(int), Required: false, Default: 1024,
            Help: "size of the large non-commercial sysex sent each open (0 to skip)"),
        new("opens", typeof(int), Required: false, Default: 3, Help: "number of open/close cycles"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceCount = MidiOut.NumberOfDevices;
        if (deviceCount == 0)
            return TestResult.Skipped("No MidiOut devices found");

        var deviceNumber = ctx.Get<int>("deviceNumber");
        if (deviceNumber < 0 || deviceNumber >= deviceCount)
            return TestResult.Fail($"deviceNumber {deviceNumber} out of range (0..{deviceCount - 1})");

        var iterations = ctx.Get<int>("iterations");
        var largeBytes = ctx.Get<int>("largeSysexBytes");
        var opens = ctx.Get<int>("opens");

        AnsiConsole.MarkupLine($"[grey]MidiOut devices:[/] {deviceCount}");
        for (var n = 0; n < deviceCount; n++)
        {
            var marker = n == deviceNumber ? "[yellow]→[/]" : " ";
            AnsiConsole.MarkupLine($"  {marker} {n}: {Markup.Escape(MidiOut.DeviceInfo(n).ProductName)}");
        }
        AnsiConsole.WriteLine();

        var largeSysex = BuildNonCommercialSysex(largeBytes);
        var deviceName = MidiOut.DeviceInfo(deviceNumber).ProductName;
        var diagnostics = new Dictionary<string, string>
        {
            ["device"] = deviceName,
            ["iterations"] = iterations.ToString(),
            ["largeSysexBytes"] = largeBytes.ToString(),
        };

        long maxSendMs = 0;
        var sw = new Stopwatch();
        for (var open = 1; open <= opens; open++)
        {
            if (ctx.Cancellation.IsCancellationRequested)
                return TestResult.Skipped($"Cancelled during open {open}", diagnostics);

            var midiOut = new MidiOut(deviceNumber);
            try
            {
                for (var i = 0; i < iterations; i++)
                {
                    sw.Restart();
                    midiOut.SendBuffer(GmSystemOn);
                    maxSendMs = Math.Max(maxSendMs, sw.ElapsedMilliseconds);
                }
                if (largeSysex != null)
                {
                    sw.Restart();
                    midiOut.SendBuffer(largeSysex);
                    diagnostics[$"open{open}LargeSendMs"] = sw.ElapsedMilliseconds.ToString();
                }
                // All Notes Off on every channel, through the short-message path.
                for (var channel = 1; channel <= 16; channel++)
                    midiOut.Send(new ControlChangeEvent(0, channel, MidiController.AllNotesOff, 0).GetAsShortMessage());
                midiOut.Reset();
            }
            catch (Exception ex)
            {
                midiOut.Dispose();
                diagnostics["failedOpen"] = open.ToString();
                return TestResult.Fail($"Open {open}: {ex.GetType().Name}: {ex.Message}", diagnostics);
            }

            midiOut.Dispose();
            // A second Dispose must be a no-op rather than calling winmm with a closed handle.
            midiOut.Dispose();
            AnsiConsole.MarkupLine($"  open {open}: {iterations} GM System On + {(largeSysex != null ? $"{largeBytes}-byte sysex" : "no large sysex")} sent");
        }

        diagnostics["maxSmallSendMs"] = maxSendMs.ToString();
        return TestResult.Pass($"{opens} opens x {iterations + (largeSysex != null ? 1 : 0)} sysex sends on {deviceName}", diagnostics);
    }

    /// <summary>
    /// A sysex using the 0x7D "non-commercial" manufacturer ID, which receivers are expected to
    /// ignore, padded with a ramp of 7-bit data bytes to the requested total length.
    /// </summary>
    private static byte[]? BuildNonCommercialSysex(int totalBytes)
    {
        if (totalBytes <= 0) return null;
        totalBytes = Math.Max(totalBytes, 3);
        var buffer = new byte[totalBytes];
        buffer[0] = 0xF0;
        buffer[1] = 0x7D;
        for (var i = 2; i < totalBytes - 1; i++)
            buffer[i] = (byte)(i & 0x7F);
        buffer[^1] = 0xF7;
        return buffer;
    }
}
