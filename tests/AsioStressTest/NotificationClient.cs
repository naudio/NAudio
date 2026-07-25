using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices.Marshalling;

namespace AsioStressTest;

[GeneratedComClass]
internal partial class NotificationClient : IMMNotificationClient
{

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) =>
        Console.WriteLine($"{nameof(OnDeviceStateChanged)}: {newState}  {deviceId}");

    public void OnDeviceAdded(string pwstrDeviceId) =>
        Console.WriteLine($"{nameof(OnDeviceAdded)}: {pwstrDeviceId}");

    public void OnDeviceRemoved(string deviceId) =>
        Console.WriteLine($"{nameof(OnDeviceRemoved)}: {deviceId}");

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
        Console.WriteLine($"{nameof(OnDefaultDeviceChanged)}: {flow}/{role} → {defaultDeviceId ?? "<none>"}");

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) =>
        Console.WriteLine($"{nameof(OnPropertyValueChanged)}: {key.formatId}/{key.propertyId}  {pwstrDeviceId}");
}
