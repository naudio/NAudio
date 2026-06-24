using NAudio.CoreAudioApi;
using Spectre.Console;

namespace NAudioConsoleTest.Shared;

static class DeviceSelector
{
    public static MMDevice? SelectRenderDevice()
    {
        return SelectDevice(DataFlow.Render, "output");
    }

    public static MMDevice? SelectCaptureDevice()
    {
        return SelectDevice(DataFlow.Capture, "input");
    }

    private static MMDevice? SelectDevice(DataFlow dataFlow, string label)
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);

        if (devices.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No active {label} devices found.[/]");
            return null;
        }

        var choices = new List<string>();
        var deviceList = new List<MMDevice>();
        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            deviceList.Add(device);
            var isDefault = IsDefaultDevice(enumerator, dataFlow, device);
            choices.Add(isDefault ? $"{device.FriendlyName} (default)" : device.FriendlyName);
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select {label} device:")
                .AddChoices(choices));

        int index = choices.IndexOf(selected);
        return deviceList[index];
    }

    private static bool IsDefaultDevice(MMDeviceEnumerator enumerator, DataFlow dataFlow, MMDevice device)
    {
        if (!enumerator.TryGetDefaultAudioEndpoint(dataFlow, Role.Console, out var defaultDevice))
            return false;
        return device.ID == defaultDevice.ID;
    }
}
