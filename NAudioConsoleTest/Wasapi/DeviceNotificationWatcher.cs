using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

// Exercises IMMNotificationClient — the only Phase 2f callback interface that's
// public and user-implemented. Logs every notification kind for ~30 seconds so
// the operator can plug/unplug a USB headset, change the default device in
// Sound Settings, or mute via the system tray and confirm the new
// [GeneratedComInterface] CCW dispatch fires for each event.
static partial class DeviceNotificationWatcher
{
    public static void Run()
    {
        using var enumerator = new MMDeviceEnumerator();
        var client = new ConsoleNotificationClient();
        int hr = enumerator.RegisterEndpointNotificationCallback(client);
        if (hr != 0)
        {
            AnsiConsole.MarkupLine($"[red]RegisterEndpointNotificationCallback failed: 0x{hr:X8}[/]");
            return;
        }

        try
        {
            AnsiConsole.MarkupLine("[green]Watching device notifications. Try plugging/unplugging a USB device, or changing the default device in Sound Settings.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to stop.[/]\n");
            // Block until the user stops the test. Notifications run on a WASAPI
            // worker thread; we're just keeping main alive.
            Console.ReadKey(intercept: true);
        }
        finally
        {
            enumerator.UnregisterEndpointNotificationCallback(client);
        }
    }

    [GeneratedComClass]
    private partial class ConsoleNotificationClient : IMMNotificationClient
    {
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) =>
            Log("OnDeviceStateChanged", $"[grey]{Markup.Escape(deviceId)}[/] → [yellow]{newState}[/]");

        public void OnDeviceAdded(string pwstrDeviceId) =>
            Log("OnDeviceAdded", $"[grey]{Markup.Escape(pwstrDeviceId)}[/]");

        public void OnDeviceRemoved(string deviceId) =>
            Log("OnDeviceRemoved", $"[grey]{Markup.Escape(deviceId)}[/]");

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
            Log("OnDefaultDeviceChanged", $"[yellow]{flow}/{role}[/] → [grey]{Markup.Escape(defaultDeviceId ?? "<none>")}[/]");

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) =>
            Log("OnPropertyValueChanged", $"[grey]{Markup.Escape(pwstrDeviceId)}[/] {key.formatId}/{key.propertyId}");

        private static void Log(string method, string body) =>
            AnsiConsole.MarkupLine($"[blue]{DateTime.Now:HH:mm:ss.fff}[/] [bold]{method}[/] {body}");
    }
}
