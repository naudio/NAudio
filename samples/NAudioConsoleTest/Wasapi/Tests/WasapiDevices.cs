using NAudio.CoreAudioApi;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Shared device enumeration and resolution for WASAPI tests. Devices are identified to the
/// outside world (CLI args, menu prompts) by their friendly name. The special value
/// <see cref="DefaultMarker"/> resolves to the OS default endpoint for the requested data flow;
/// <see cref="RoutingMarker"/> selects automatic stream routing (no fixed endpoint — the stream
/// follows the default device and re-routes when it changes).
/// </summary>
internal static class WasapiDevices
{
    public const string DefaultMarker = "default";
    public const string RoutingMarker = "default (auto stream routing)";

    public static IReadOnlyList<string> RenderDeviceNames() => DeviceNames(DataFlow.Render);
    public static IReadOnlyList<string> CaptureDeviceNames() => DeviceNames(DataFlow.Capture);

    public static MMDevice? ResolveRender(string name) => Resolve(DataFlow.Render, name);
    public static MMDevice? ResolveCapture(string name) => Resolve(DataFlow.Capture, name);

    /// <summary>
    /// True when the selected device name requests automatic stream routing rather than a fixed
    /// endpoint. In that case there is no <see cref="MMDevice"/> — build the player/recorder via
    /// <c>WithDefaultDeviceStreamRouting().BuildAsync()</c> instead of resolving a device.
    /// </summary>
    public static bool IsRoutingMarker(string name) =>
        string.Equals(name, RoutingMarker, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> DeviceNames(DataFlow flow)
    {
        var names = new List<string> { DefaultMarker, RoutingMarker };
        using var enumerator = new MMDeviceEnumerator();
        foreach (var d in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            names.Add(d.FriendlyName);
        return names;
    }

    private static MMDevice? Resolve(DataFlow flow, string name)
    {
        using var enumerator = new MMDeviceEnumerator();
        if (string.IsNullOrWhiteSpace(name)
            || string.Equals(name, DefaultMarker, StringComparison.OrdinalIgnoreCase))
        {
            return enumerator.TryGetDefaultAudioEndpoint(flow, Role.Multimedia, out var def) ? def : null;
        }

        foreach (var d in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            if (string.Equals(d.FriendlyName, name, StringComparison.OrdinalIgnoreCase))
                return d;
        }
        return null;
    }
}
