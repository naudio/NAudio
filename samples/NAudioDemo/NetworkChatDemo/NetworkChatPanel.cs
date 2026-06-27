using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudioDemo.Utils;

namespace NAudioDemo.NetworkChatDemo;

public partial class NetworkChatPanel : UserControl
{
    private readonly MMDeviceEnumerator deviceEnumerator = new();
    private INetworkChatCodec selectedCodec;
    private volatile bool connected;
    private NetworkAudioPlayer player;
    private NetworkAudioSender audioSender;

    public NetworkChatPanel()
    {
        // use reflection to find all the codecs
        var codecs = ReflectionHelper.CreateAllInstancesOf<INetworkChatCodec>();

        InitializeComponent();
        PopulateInputDevicesCombo();
        PopulateCodecsCombo(codecs);
        Disposed += OnPanelDisposed;
    }

    private void OnPanelDisposed(object sender, EventArgs e)
    {
        Disconnect();
        deviceEnumerator.Dispose();
    }

    private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
    {
        var sorted = from codec in codecs
                     where codec.IsAvailable
                     orderby codec.BitsPerSecond ascending
                     select codec;

        foreach (var codec in sorted)
        {
            var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
            var text = $"{codec.Name} ({bitRate})";
            comboBoxCodecs.Items.Add(new CodecComboItem { Text = text, Codec = codec });
        }

        // Default to Opus wide-band if present: it sounds dramatically better than the legacy
        // telephony codecs at a comparable bitrate and works the same on every platform.
        var defaultIndex = FindCodecIndex("Opus Wide");
        comboBoxCodecs.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
    }

    private int FindCodecIndex(string namePrefix)
    {
        for (int i = 0; i < comboBoxCodecs.Items.Count; i++)
        {
            if (((CodecComboItem)comboBoxCodecs.Items[i]).Codec.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private class CodecComboItem
    {
        public string Text { get; set; }
        public INetworkChatCodec Codec { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }

    private void PopulateInputDevicesCombo()
    {
        var captureDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
        comboBoxInputDevices.DisplayMember = nameof(MMDevice.FriendlyName);
        comboBoxInputDevices.DataSource = captureDevices;
        if (captureDevices.Length == 0)
        {
            return;
        }
        try
        {
            // Pre-select the user's default communications microphone where there is one.
            var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            var index = Array.FindIndex(captureDevices, d => d.ID == defaultDevice.ID);
            comboBoxInputDevices.SelectedIndex = Math.Max(0, index);
        }
        catch
        {
            comboBoxInputDevices.SelectedIndex = 0;
        }
    }

    private void buttonStartStreaming_Click(object sender, EventArgs e)
    {
        if (!connected)
        {
            if (TryConnect())
            {
                buttonStartStreaming.Text = "Stop";
            }
        }
        else
        {
            Disconnect();
            buttonStartStreaming.Text = "Start Streaming";
        }
    }

    private bool TryConnect()
    {
        var remoteHost = textBoxRemoteHost.Text.Trim();
        if (remoteHost.Length == 0)
        {
            MessageBox.Show("Please enter the remote host name or IP address.", "Network Chat");
            return false;
        }
        if (!int.TryParse(textBoxRemotePort.Text, out var remotePort) || remotePort is < 1 or > 65535)
        {
            MessageBox.Show("Please enter a valid remote port (1-65535).", "Network Chat");
            return false;
        }
        if (!int.TryParse(textBoxListenPort.Text, out var listenPort) || listenPort is < 1 or > 65535)
        {
            MessageBox.Show("Please enter a valid listen port (1-65535).", "Network Chat");
            return false;
        }
        if (comboBoxInputDevices.SelectedItem is not MMDevice inputDevice)
        {
            MessageBox.Show("No audio input device is available.", "Network Chat");
            return false;
        }

        selectedCodec = ((CodecComboItem)comboBoxCodecs.SelectedItem).Codec;
        try
        {
            Connect(remoteHost, remotePort, listenPort, inputDevice, selectedCodec);
            return true;
        }
        catch (Exception ex)
        {
            // Clean up anything that started before the failure (e.g. the listen port was in use).
            Disconnect();
            MessageBox.Show($"Could not start streaming: {ex.Message}", "Network Chat");
            return false;
        }
    }

    private void Connect(string remoteHost, int remotePort, int listenPort, MMDevice inputDevice, INetworkChatCodec codec)
    {
        // Audio in, audio out: we listen on our own port and send to the remote endpoint. UDP is
        // the right transport for real-time audio - a lost datagram is a brief glitch rather than a
        // stall, and there is no connection to set up so peers can start in any order.
        var receiver = new UdpAudioReceiver(listenPort);
        player = new NetworkAudioPlayer(codec, receiver);

        var sender = new UdpAudioSender(remoteHost, remotePort);
        audioSender = new NetworkAudioSender(codec, inputDevice, sender);

        connected = true;
    }

    private void Disconnect()
    {
        connected = false;

        player?.Dispose();
        player = null;
        audioSender?.Dispose();
        audioSender = null;

        // a bit naughty but we have designed the codecs to support multiple calls to Dispose,
        // recreating their resources if Encode/Decode called again
        selectedCodec?.Dispose();
    }
}

public class NetworkChatPanelPlugin : INAudioDemoPlugin
{
    public string Name => "Network Chat";

    public Control CreatePanel()
    {
        return new NetworkChatPanel();
    }
}
