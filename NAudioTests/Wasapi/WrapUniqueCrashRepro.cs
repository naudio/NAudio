using System;
using System.Collections.Generic;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Wasapi
{
    /// <summary>
    /// Attempted headless reproducer for the WrapUnique GC-finalization crash. As of
    /// 2026-04-28, this test does NOT yet trip the manual demo crash even with
    /// <c>GC.SuppressFinalize(wrapper)</c> commented out in <c>ComActivation.WrapUnique</c>.
    /// Marked <see cref="ExplicitAttribute"/> so it doesn't run as a false green in CI.
    ///
    /// <para>
    /// Manual repro in NAudioDemo: play audio with the WasapiPlayer plugin, stop, switch
    /// to the Volume Mixer demo. Volume Mixer dies during its WinForms layout pass with
    /// a __fastfail (process kill bypassing every managed handler) — last log line is
    /// consistently <c>flowLayoutPanelApps.Controls.Add(panel)</c> for the SECOND
    /// session panel. The demo's <c>DisposeCurrentDemo</c> does <c>GC.Collect()</c>
    /// after disposing the previous panel; that finalizes never-disposed combo
    /// MMDevices from the WasapiOut settings panel.
    /// </para>
    ///
    /// <para>
    /// What this test tries: the player → drop combos → GC.Collect (no wait, race
    /// allowed) → re-enter Volume Mixer COM workload → heap-stress sequence, looped
    /// 10x. With suppression on AND off, runs to completion in ~30s. So either the
    /// crash needs the real WinForms message pump (control creation, GDI, message
    /// dispatch, WM_PAINT) that this test lacks, or it needs specific external state
    /// (process count, session count) that the dev box happens to provide.
    /// </para>
    ///
    /// <para>
    /// Suggested next attempts when picking this up: (1) run an actual
    /// <c>Application.Run</c> message pump on the STA thread with a hidden Form and
    /// reproduce panel switches via Form button clicks; (2) attach WinDbg to NAudioDemo
    /// and capture the failing thread when the manual repro fires (the original
    /// handover already proposed the <c>SetUnhandledExceptionFilter</c> +
    /// <c>MiniDumpWriteDump</c> scaffolding for this); (3) bisect the
    /// 22-call-site WrapUnique sweep against the NAudioDemo manual repro to find the
    /// minimum suppression set actually load-bearing.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    [Explicit("Does not currently trip the manual demo crash; see XML doc.")]
    public class WrapUniqueCrashRepro
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Player_Then_GC_Then_VolumeMixer_DoesNotCrash()
        {
            OSUtils.RequireVista();

            EnsureRenderEndpointAvailable();

            // The manual demo crash needs three things in sequence: WasapiPlayer activity,
            // a forced GC that finalizes never-disposed combo MMDevice wrappers, then
            // re-entry into the same endpoints' COM state via the Volume Mixer panel.
            // We loop the whole sequence multiple times because in the demo this happens
            // across multiple panel switches.
            for (int iteration = 0; iteration < 10; iteration++)
            {
                ExerciseWasapiPlayerBriefly();
                DropUndisposedComboMmDevices();

                // The demo's GC.Collect() canary in DisposeCurrentDemo. Crucially we do
                // NOT wait for finalizers — the demo doesn't either; the next panel's
                // layout pass starts immediately while the finalizer thread is still
                // running. That race is the critical aspect of the manual repro.
                GC.Collect();

                DoVolumeMixerPanelWork();

                // Memory-stress: force lots of small heap allocations to shake out any
                // latent heap corruption that the finalizer activity may have caused.
                // The demo's WinForms layout pass touches massive amounts of memory
                // (control creation, message dispatch, GDI buffers); without that, a
                // smashed allocation might not be hit on its own.
                StressHeap();
            }
        }

        private static void StressHeap()
        {
            // Touch ~16 MB of small allocations. If finalizer activity corrupted heap
            // metadata, these allocations are likely to surface it.
            var pile = new List<byte[]>(4096);
            for (int i = 0; i < 4096; i++)
            {
                var buf = new byte[4096];
                buf[0] = (byte)i;
                buf[buf.Length - 1] = (byte)(i ^ 0xFF);
                pile.Add(buf);
            }
            // Read every buffer back to force the JIT to actually emit the loads.
            int sink = 0;
            foreach (var buf in pile) sink ^= buf[0] ^ buf[buf.Length - 1];
            GC.KeepAlive(sink);
        }

        private static void EnsureRenderEndpointAvailable()
        {
            using var enumerator = new MMDeviceEnumerator();
            if (!enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
            {
                Assert.Inconclusive("No default render endpoint available; cannot exercise WasapiPlayer.");
            }
        }

        private static void ExerciseWasapiPlayerBriefly()
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            using var player = new WasapiPlayerBuilder()
                .WithDevice(device)
                .WithSharedMode()
                .WithLatency(100)
                .Build();

            // Use the device's own mix format so Initialize succeeds without resampling.
            var silence = new SilenceProvider(player.DeviceMixFormat);
            player.Init(silence);
            player.Play();
            Thread.Sleep(300);
            player.Stop();
        }

        private static void DropUndisposedComboMmDevices()
        {
            // The demo's WasapiOutSettingsPanel populates a combo box with MMDevices
            // and the VolumePanel's cmbDevice does the same — neither disposes its items.
            // We deliberately leak two batches: render and capture, FriendlyName and
            // IconPath read on each so the PropertyStore wrapper exists too.
            var leakedDevices = new List<MMDevice>();
            var leakedEnumerators = new List<MMDeviceEnumerator>();
            var leakedCollections = new List<MMDeviceCollection>();

            foreach (var flow in new[] { DataFlow.Render, DataFlow.Capture })
            {
                var enumerator = new MMDeviceEnumerator();
                leakedEnumerators.Add(enumerator);
                var devices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
                leakedCollections.Add(devices);
                foreach (var device in devices)
                {
                    _ = device.FriendlyName;
                    try { _ = device.IconPath; } catch { /* not all devices */ }
                    // Activate endpoint volume — this registers an
                    // AudioEndpointVolumeCallback CCW. If we never Dispose, the
                    // callback stays registered on the COM object even after the
                    // managed wrapper is collected — exactly the lifetime hazard
                    // the manual demo exhibits.
                    try { _ = device.AudioEndpointVolume; } catch { }
                    leakedDevices.Add(device);
                }
            }

            // Drop ALL strong references — wrappers are now eligible for finalization.
            // We deliberately do NOT call Dispose on any of them. This is what the demo
            // does today (combos hold MMDevices in Items; the panel never disposes them).
            leakedDevices.Clear();
            leakedCollections.Clear();
            leakedEnumerators.Clear();
        }

        private static void DoVolumeMixerPanelWork()
        {
            // Mirror VolumePanel construction: it activates the device, queries
            // endpoint volume + master scalar + mute, populates a device combo,
            // and then for each session reads SimpleAudioVolume + IsSystemSoundsSession
            // + IconPath + GetProcessID + RegisterEventClient. Each of these touches
            // the same endpoint COM state that just had wrappers finalized.
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            _ = device.FriendlyName;
            var endpointVolume = device.AudioEndpointVolume;
            _ = endpointVolume.MasterVolumeLevelScalar;
            _ = endpointVolume.Mute;

            // Mirror the cmbDevice combo population in VolumePanel_Load — these
            // MMDevices also get added to a combo and never disposed.
            var deviceLeak = new List<MMDevice>();
            using (var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                foreach (var d in devices)
                {
                    _ = d.FriendlyName;
                    deviceLeak.Add(d);
                }
            }

            var sessionManager = device.AudioSessionManager;
            sessionManager.RefreshSessions();
            var sessions = sessionManager.Sessions;
            if (sessions != null)
            {
                int n = sessions.Count;
                for (int i = 0; i < n; i++)
                {
                    var session = sessions[i];
                    // Mirror VolumePanel ctor work — read everything the session panel
                    // reads during construction. Don't dispose; mirror the demo.
                    try { _ = session.IsSystemSoundsSession; } catch { }
                    try { _ = session.GetProcessID; } catch { }
                    try { _ = session.DisplayName; } catch { }
                    try { _ = session.IconPath; } catch { }
                    try { _ = session.SimpleAudioVolume.Volume; } catch { }
                    try { _ = session.SimpleAudioVolume.Mute; } catch { }
                    try { _ = session.AudioMeterInformation?.MasterPeakValue; } catch { }
                    try { session.RegisterEventClient(new NoopHandler()); } catch { }
                }
            }

            deviceLeak.Clear();
        }

        private sealed class NoopHandler : IAudioSessionEventsHandler
        {
            public void OnVolumeChanged(float volume, bool isMuted) { }
            public void OnDisplayNameChanged(string displayName) { }
            public void OnIconPathChanged(string iconPath) { }
            public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
            public void OnGroupingParamChanged(ref Guid groupingId) { }
            public void OnStateChanged(AudioSessionState state) { }
            public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { }
        }
    }
}
