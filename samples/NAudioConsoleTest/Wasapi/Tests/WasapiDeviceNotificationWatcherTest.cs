using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Exercises <c>IMMNotificationClient</c> — the only Phase 2f callback interface that's public
/// and user-implemented. Watches for <c>duration</c> while the operator plugs/unplugs a USB
/// device, changes the default endpoint, mutes via the tray, etc., confirming each notification
/// kind dispatches through the new <c>[GeneratedComInterface]</c> CCW.
/// </summary>
internal sealed partial class WasapiDeviceNotificationWatcherTest : IConsoleTest
{
    public string Id => "Wasapi.DeviceNotificationWatcher";
    public string Description => "Watch for device notifications (plug/unplug, default change, etc.)";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Watch device notifications", Group: "Callbacks", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(30),
            Help: "how long to watch for notifications"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var duration = ctx.Get<TimeSpan>("duration");

        using var enumerator = new MMDeviceEnumerator();
        var notifyCount = 0;
        var client = new ConsoleNotificationClient(_ => Interlocked.Increment(ref notifyCount));

        var hr = enumerator.RegisterEndpointNotificationCallback(client);
        if (hr != 0)
            return TestResult.Fail($"RegisterEndpointNotificationCallback failed: 0x{hr:X8}");

        try
        {
            AnsiConsole.MarkupLine(
                $"[green]Watching for {duration.TotalSeconds:F0}s.[/] " +
                "[dim]Plug/unplug a USB device, or change the default in Sound Settings. Ctrl+C to stop early.[/]\n");

            // Notifications run on a WASAPI worker thread; we just block until the duration or cancellation.
            ctx.Cancellation.WaitHandle.WaitOne(duration);
        }
        finally
        {
            enumerator.UnregisterEndpointNotificationCallback(client);
        }

        var diagnostics = new Dictionary<string, string>
        {
            ["durationSec"] = duration.TotalSeconds.ToString("F0"),
            ["notifications"] = notifyCount.ToString(),
        };

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped($"Cancelled after {notifyCount} notification(s)");

        return TestResult.Pass($"Watched {duration.TotalSeconds:F0}s, {notifyCount} notification(s) received",
            diagnostics);
    }

    [GeneratedComClass]
    private partial class ConsoleNotificationClient : IMMNotificationClient
    {
        private readonly Action<string> onAny;
        public ConsoleNotificationClient(Action<string> onAny) => this.onAny = onAny;

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            onAny("OnDeviceStateChanged");
            Log("OnDeviceStateChanged", $"[grey]{Markup.Escape(deviceId)}[/] → [yellow]{newState}[/]");
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            onAny("OnDeviceAdded");
            Log("OnDeviceAdded", $"[grey]{Markup.Escape(pwstrDeviceId)}[/]");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            onAny("OnDeviceRemoved");
            Log("OnDeviceRemoved", $"[grey]{Markup.Escape(deviceId)}[/]");
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            onAny("OnDefaultDeviceChanged");
            Log("OnDefaultDeviceChanged", $"[yellow]{flow}/{role}[/] → [grey]{Markup.Escape(defaultDeviceId ?? "<none>")}[/]");
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            onAny("OnPropertyValueChanged");
            Log("OnPropertyValueChanged", $"[grey]{Markup.Escape(pwstrDeviceId)}[/] {key.formatId}/{key.propertyId}");
        }

        private static void Log(string method, string body) =>
            AnsiConsole.MarkupLine($"[blue]{DateTime.Now:HH:mm:ss.fff}[/] [bold]{method}[/] {body}");
    }
}
