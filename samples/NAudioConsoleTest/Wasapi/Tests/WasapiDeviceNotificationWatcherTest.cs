using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Exercises <c>MMDeviceNotificationClient</c> — the high-level, event-based way to observe endpoint
/// changes. Watches for <c>duration</c> while the operator plugs/unplugs a USB device, changes the
/// default endpoint, mutes via the tray, etc., confirming each notification kind is dispatched.
/// </summary>
internal sealed class WasapiDeviceNotificationWatcherTest : IConsoleTest
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

        // Notifications run on a WASAPI worker thread; keep events off the SynchronizationContext so we
        // log directly from the worker (this is a console app with no UI thread to marshal to).
        using var notifications = enumerator.CreateNotificationClient(useSynchronizationContext: false);
        notifications.DeviceStateChanged += (_, e) =>
        {
            Interlocked.Increment(ref notifyCount);
            Log("DeviceStateChanged", $"[grey]{Markup.Escape(e.DeviceId)}[/] → [yellow]{e.NewState}[/]");
        };
        notifications.DeviceAdded += (_, e) =>
        {
            Interlocked.Increment(ref notifyCount);
            Log("DeviceAdded", $"[grey]{Markup.Escape(e.DeviceId)}[/]");
        };
        notifications.DeviceRemoved += (_, e) =>
        {
            Interlocked.Increment(ref notifyCount);
            Log("DeviceRemoved", $"[grey]{Markup.Escape(e.DeviceId)}[/]");
        };
        notifications.DefaultDeviceChanged += (_, e) =>
        {
            Interlocked.Increment(ref notifyCount);
            Log("DefaultDeviceChanged", $"[yellow]{e.Flow}/{e.Role}[/] → [grey]{Markup.Escape(e.DeviceId ?? "<none>")}[/]");
        };
        notifications.PropertyValueChanged += (_, e) =>
        {
            Interlocked.Increment(ref notifyCount);
            Log("PropertyValueChanged", $"[grey]{Markup.Escape(e.DeviceId)}[/] {e.PropertyKey.formatId}/{e.PropertyKey.propertyId}");
        };

        AnsiConsole.MarkupLine(
            $"[green]Watching for {duration.TotalSeconds:F0}s.[/] " +
            "[dim]Plug/unplug a USB device, or change the default in Sound Settings. Ctrl+C to stop early.[/]\n");

        // Block until the duration elapses or the user cancels.
        ctx.Cancellation.WaitHandle.WaitOne(duration);

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

    private static void Log(string method, string body) =>
        AnsiConsole.MarkupLine($"[blue]{DateTime.Now:HH:mm:ss.fff}[/] [bold]{method}[/] {body}");
}
