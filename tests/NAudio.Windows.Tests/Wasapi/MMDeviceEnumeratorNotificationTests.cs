using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Windows.Tests.Utils;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
[Category("IntegrationTest")]
[Platform("Win")]
public class MMDeviceEnumeratorNotificationTests
{
    // Regression cover for the access violation (0xC0000005 in ComInterfaceDispatch.GetInstance ->
    // ABI_OnPropertyValueChanged) that crashed the process when an endpoint notification fired after
    // a client was registered. Windows does NOT AddRef the IMMNotificationClient passed to
    // RegisterEndpointNotificationCallback, so the source-generated CCW was collectable; once the GC
    // ran, the next notification (e.g. a master-volume change) dispatched into a dead object.
    //
    // The test deliberately keeps NO managed reference to the client: only the fix, which retains the
    // CCW reference inside MMDeviceEnumerator for the registration's lifetime, keeps it alive across
    // the forced GC. If the fix regresses, changing the volume below faults the whole test host — an
    // uncatchable native crash — which is the intended failure signal.
    [Test]
    public void RegisteredClientSurvivesGcAndReceivesNotifications()
    {
        OSUtils.RequireVista();

        RegressionNotificationClient.Reset();

        using var enumerator = new MMDeviceEnumerator();
        if (!enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device))
        {
            Assert.Ignore("No default render endpoint available on this machine");
            return;
        }

        // Register a client we hold no reference to. Post-fix the enumerator roots it; pre-fix it is
        // eligible for collection and the GC below leaves native holding a dangling CCW pointer.
        Assert.That(enumerator.RegisterEndpointNotificationCallback(new RegressionNotificationClient()),
            Is.GreaterThanOrEqualTo(0), "RegisterEndpointNotificationCallback failed");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var volume = device.AudioEndpointVolume;
        float original = volume.MasterVolumeLevelScalar;
        bool originalMute = volume.Mute;
        try
        {
            // Changing the endpoint's volume/mute raises OnPropertyValueChanged on the worker thread —
            // this is the exact stimulus that used to dispatch into the collected CCW and crash.
            float probe = original >= 0.5f ? original - 0.05f : original + 0.05f;
            volume.MasterVolumeLevelScalar = Math.Clamp(probe, 0f, 1f);
            volume.Mute = !originalMute;

            bool fired = RegressionNotificationClient.Notified.Wait(TimeSpan.FromSeconds(5));
            Assert.That(fired, Is.True,
                "Expected at least one endpoint notification after changing the master volume/mute");
        }
        finally
        {
            try { volume.Mute = originalMute; } catch { /* best effort restore */ }
            try { volume.MasterVolumeLevelScalar = original; } catch { /* best effort restore */ }
            device.Dispose();
        }
    }
}

// Namespace-level partial class so the [GeneratedComClass] CCW source generator can emit its vtable.
// Observation is via static members so the test can watch for callbacks without holding a reference to
// the instance (which would defeat the point of the GC step above).
[GeneratedComClass]
internal partial class RegressionNotificationClient : IMMNotificationClient
{
    public static readonly ManualResetEventSlim Notified = new(false);

    public static void Reset() => Notified.Reset();

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => Notified.Set();
    public void OnDeviceAdded(string pwstrDeviceId) => Notified.Set();
    public void OnDeviceRemoved(string deviceId) => Notified.Set();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => Notified.Set();
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) => Notified.Set();
}
