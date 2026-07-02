using System.Windows.Forms;

namespace NAudioDemo.MixingCaptureDemo;

/// <summary>
/// Demonstrates capturing several WASAPI sources (microphones and/or loopback) at once and
/// live-mixing them into a single WAV file using NAudio.Extras' RealtimeCaptureMixer.
/// </summary>
public class MixingCaptureDemoPlugin : INAudioDemoPlugin
{
    public string Name => "Mixing Capture (mic + system audio)";

    public Control CreatePanel() => new MixingCapturePanel();
}
