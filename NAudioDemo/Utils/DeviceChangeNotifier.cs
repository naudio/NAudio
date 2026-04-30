using System;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudioDemo.Utils
{
    // Shared helper that registers an IMMNotificationClient against a supplied
    // MMDeviceEnumerator and raises a single .NET event whenever a relevant
    // notification fires — device add/remove/state-change/default-change. The
    // event is marshalled to the calling thread via SynchronizationContext so
    // panels can refresh combos without worrying about WASAPI worker threads.
    //
    // Used by RecordingPanel and the AudioPlaybackDemo's WASAPI settings panel
    // to auto-repopulate their device combos. Also exercises the public
    // [GeneratedComInterface] IMMNotificationClient CCW path during normal
    // demo usage — if that path silently regresses, the combos go stale.
    sealed partial class DeviceChangeNotifier : IDisposable
    {
        private readonly MMDeviceEnumerator enumerator;
        private readonly Client client;
        private readonly System.Threading.SynchronizationContext syncContext;
        private bool disposed;

        public event Action DevicesChanged;

        public DeviceChangeNotifier(MMDeviceEnumerator enumerator)
        {
            this.enumerator = enumerator;
            syncContext = System.Threading.SynchronizationContext.Current;
            client = new Client(this);
            enumerator.RegisterEndpointNotificationCallback(client);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { enumerator.UnregisterEndpointNotificationCallback(client); }
            catch { /* best effort */ }
        }

        private void Raise()
        {
            var handler = DevicesChanged;
            if (handler == null) return;
            if (syncContext != null)
                syncContext.Post(_ => handler(), null);
            else
                handler();
        }

        [GeneratedComClass]
        private partial class Client : IMMNotificationClient
        {
            private readonly DeviceChangeNotifier owner;
            public Client(DeviceChangeNotifier owner) => this.owner = owner;

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) => owner.Raise();
            public void OnDeviceAdded(string pwstrDeviceId) => owner.Raise();
            public void OnDeviceRemoved(string deviceId) => owner.Raise();
            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => owner.Raise();
            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { /* high frequency, ignore for refresh */ }
        }
    }
}
