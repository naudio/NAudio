using System;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;

namespace NAudioWpfDemo.Utils
{
    /// <summary>
    /// Creates an audio output for the sampler demos. Prefers the modern WASAPI
    /// player (shared mode, the device's default — well-tested — path), and falls
    /// back to <see cref="WaveOut"/> if WASAPI can't be initialised on this device.
    /// Playback-thread errors are surfaced to <paramref name="onError"/> on the UI
    /// thread, so a backend failure shows a message instead of vanishing silently.
    /// </summary>
    static class SamplerPlayback
    {
        public static IWavePlayer Create(ISampleProvider source, Action<Exception> onError)
        {
            void Stopped(object sender, StoppedEventArgs e)
            {
                if (e.Exception != null) RaiseOnUi(() => onError?.Invoke(e.Exception));
            }

            try
            {
                var wasapi = new WasapiPlayerBuilder().Build();
                wasapi.PlaybackStopped += Stopped;
                wasapi.Init(source.ToWaveProvider());
                return wasapi;
            }
            catch (Exception)
            {
                // WASAPI unavailable/unsupported on this device — use the classic path
                var waveOut = new WaveOut();
                waveOut.PlaybackStopped += Stopped;
                waveOut.Init(source.ToWaveProvider());
                return waveOut;
            }
        }

        private static void RaiseOnUi(Action action)
        {
            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }
    }
}
