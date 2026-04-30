using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

// AOT smoke app. Exercises both directions of the source-generated COM bridging:
//
// (1) RCW direction — IPropertyStore + PropVariant (Phase 2d) and the
//     CoCreateInstance / GetOrCreateObjectForComInstance projection path
//     (Phase 2e). Enumerates render endpoints and reads VT_LPWSTR / VT_UI4 /
//     VT_BLOB properties.
//
// (2) CCW direction — the [GeneratedComClass] callback dispatch reworked in
//     Phase 2f. Registers an IMMNotificationClient and an
//     IAudioEndpointVolumeCallback (via AudioEndpointVolume.OnVolumeNotification),
//     drives the master volume a few times, and counts firings. The Phase 2f
//     QI-for-IID fix is tested here under genuine NativeAOT — pure trim wasn't
//     sufficient because trim still permits reflection-based vtable inference;
//     PublishAot does not, so this run is the strongest "callbacks survive
//     whole-program analysis" signal we have.

Console.WriteLine("=== Phase 2d / 2e: RCW direction (property reads) ===\n");

var enumerator = new MMDeviceEnumerator();
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

Console.WriteLine($"Found {devices.Count} active render endpoint(s):");
foreach (MMDevice device in devices)
{
    Console.WriteLine();
    Console.WriteLine($"  {device.FriendlyName}");
    Console.WriteLine($"    ID:           {device.ID}");
    Console.WriteLine($"    State:        {device.State}");
    Console.WriteLine($"    DataFlow:     {device.DataFlow}");

    // Hits VT_LPWSTR — guid is stored as a stringified GUID
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_GUID))
    {
        Console.WriteLine($"    EndpointGUID: {device.Properties[PropertyKeys.PKEY_AudioEndpoint_GUID].Value}");
    }

    // Hits VT_UI4
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_FormFactor))
    {
        Console.WriteLine($"    FormFactor:   {device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor].Value}");
    }

    // Hits VT_BLOB (WAVEFORMATEX)
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEngine_DeviceFormat))
    {
        var blob = (byte[])device.Properties[PropertyKeys.PKEY_AudioEngine_DeviceFormat].Value;
        Console.WriteLine($"    DeviceFormat: {blob.Length}-byte WAVEFORMAT blob");
    }

    Console.WriteLine($"    Total properties: {device.Properties.Count}");
}

Console.WriteLine();
Console.WriteLine("=== Phase 2f: CCW direction (callback dispatch) ===\n");

// IMMNotificationClient — no easy automated trigger, just confirm registration
// + unregistration round-trip without throwing. Plug/unplug a USB device while
// this runs to see live notifications.
var notificationClient = new SmokeNotificationClient();
int regHr = enumerator.RegisterEndpointNotificationCallback(notificationClient);
Console.WriteLine($"  RegisterEndpointNotificationCallback HRESULT: 0x{regHr:X8} ({(regHr == 0 ? "OK" : "FAIL")})");

// IAudioEndpointVolumeCallback — drive the default render endpoint and count callbacks.
var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
var endpointVolume = defaultDevice.AudioEndpointVolume;
float originalLevel = endpointVolume.MasterVolumeLevelScalar;
Console.WriteLine($"  Default endpoint: {defaultDevice.FriendlyName} (master={originalLevel:F3})");

int notifyCount = 0;
endpointVolume.OnVolumeNotification += _ => Interlocked.Increment(ref notifyCount);

try
{
    float[] levels = { 0.40f, 0.60f, 0.40f, 0.50f };
    foreach (var lvl in levels)
    {
        endpointVolume.MasterVolumeLevelScalar = lvl;
        Thread.Sleep(75);
    }
    Thread.Sleep(200); // let stragglers land
    Console.WriteLine($"  Drove {levels.Length} master-volume changes, {notifyCount} OnVolumeNotification callbacks fired.");
    Console.WriteLine(notifyCount > 0
        ? "  CCW dispatch under PublishAot: OK"
        : "  CCW dispatch under PublishAot: FAIL — zero callbacks fired (registration didn't take or AOT trimmed the dispatch path)");
}
finally
{
    try { endpointVolume.MasterVolumeLevelScalar = originalLevel; } catch { }
    Thread.Sleep(150);
    enumerator.UnregisterEndpointNotificationCallback(notificationClient);
    endpointVolume.Dispose();
    defaultDevice.Dispose();
}

[GeneratedComClass]
partial class SmokeNotificationClient : IMMNotificationClient
{
    public void OnDeviceStateChanged(string deviceId, DeviceState newState) =>
        Console.WriteLine($"    [IMMNotificationClient] OnDeviceStateChanged {newState} {deviceId}");
    public void OnDeviceAdded(string pwstrDeviceId) =>
        Console.WriteLine($"    [IMMNotificationClient] OnDeviceAdded {pwstrDeviceId}");
    public void OnDeviceRemoved(string deviceId) =>
        Console.WriteLine($"    [IMMNotificationClient] OnDeviceRemoved {deviceId}");
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
        Console.WriteLine($"    [IMMNotificationClient] OnDefaultDeviceChanged {flow}/{role} → {defaultDeviceId}");
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
