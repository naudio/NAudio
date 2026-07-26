using System;
using System.Threading;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using NAudio.Windows.Tests.Utils;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
[Category("IntegrationTest")]
[Platform("Win")]
public class MMDeviceEnumeratorNotificationTests
{
    // Regression cover for the access violation (0xC0000005 in ComInterfaceDispatch.GetInstance ->
    // ABI_OnPropertyValueChanged) that crashed the process when an endpoint notification fired after a
    // client was registered. Windows does not AddRef the underlying IMMNotificationClient, so its CCW
    // was collectable; once the GC ran, the next notification (e.g. a master-volume change) dispatched
    // into a dead object. MMDeviceEnumerator now retains the CCW reference for the registration's
    // lifetime; if that regresses, the volume change below faults the whole test host.
    [Test]
    public void NotificationClientSurvivesGcAndReceivesNotifications()
    {
        OSUtils.RequireVista();

        using var enumerator = new MMDeviceEnumerator();
        if (!enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device))
        {
            Assert.Ignore("No default render endpoint available on this machine");
            return;
        }

        using var notified = new ManualResetEventSlim(false);
        // useSynchronizationContext: false -> events fire on the audio worker thread, which is where the
        // dangling-CCW crash used to happen; setting the signal there is exactly the path under test.
        using var notifications = enumerator.CreateNotificationClient(useSynchronizationContext: false);
        notifications.PropertyValueChanged += (_, _) => notified.Set();

        // Force the collection that used to reclaim the CCW's managed target.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var volume = device.AudioEndpointVolume;
        float original = volume.MasterVolumeLevelScalar;
        bool originalMute = volume.Mute;
        try
        {
            // Changing the endpoint volume/mute raises OnPropertyValueChanged on the worker thread.
            float probe = original >= 0.5f ? original - 0.05f : original + 0.05f;
            volume.MasterVolumeLevelScalar = Math.Clamp(probe, 0f, 1f);
            volume.Mute = !originalMute;

            Assert.That(notified.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Expected a property-value-changed notification after changing the master volume/mute");
        }
        finally
        {
            try { volume.Mute = originalMute; } catch { /* best effort restore */ }
            try { volume.MasterVolumeLevelScalar = original; } catch { /* best effort restore */ }
            device.Dispose();
        }
    }
}
