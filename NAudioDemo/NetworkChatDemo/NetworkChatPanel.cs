using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using System.Net;
using NAudioDemo.Utils;

namespace NAudioDemo.NetworkChatDemo
{
    public partial class NetworkChatPanel : UserControl
    {
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
            comboBoxProtocol.Items.Add("UDP");
            comboBoxProtocol.Items.Add("TCP");
            comboBoxProtocol.SelectedIndex = 0;
            Disposed += OnPanelDisposed;
        }

        void OnPanelDisposed(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
        {
            var sorted = from codec in codecs 
                         where codec.IsAvailable
                         orderby codec.BitsPerSecond ascending 
                         select codec;
            
            foreach(var codec in sorted)
            {
                var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
                var text = $"{codec.Name} ({bitRate})";
                comboBoxCodecs.Items.Add(new CodecComboItem { Text=text, Codec=codec });
            }
            comboBoxCodecs.SelectedIndex = 0;
        }

        class CodecComboItem
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
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                comboBoxInputDevices.Items.Add(capabilities.ProductName);
            }
            if (comboBoxInputDevices.Items.Count > 0)
            {
                comboBoxInputDevices.SelectedIndex = 0;
            }
        }

        private void buttonStartStreaming_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(textBoxIPAddress.Text), int.Parse(textBoxPort.Text));
                int inputDeviceNumber = comboBoxInputDevices.SelectedIndex;
                selectedCodec = ((CodecComboItem)comboBoxCodecs.SelectedItem).Codec;
                Connect(endPoint, inputDeviceNumber,selectedCodec);
                buttonStartStreaming.Text = "Disconnect";
            }
            else
            {
                Disconnect();
                buttonStartStreaming.Text = "Connect";
            }
        }

        private void Connect(IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
        {
            var receiver = (comboBoxProtocol.SelectedIndex == 0)
                ? (IAudioReceiver) new UdpAudioReceiver(endPoint.Port)
                : new TcpAudioReceiver(endPoint.Port);
            var sender = (comboBoxProtocol.SelectedIndex == 0)
                ? (IAudioSender)new UdpAudioSender(endPoint)
                : new TcpAudioSender(endPoint);

            player = new NetworkAudioPlayer(codec, receiver);
            audioSender = new NetworkAudioSender(codec, inputDeviceNumber, sender);
            connected = true;
        }

        private void Disconnect()
        {
            if (connected)
            {
                connected = false;

                player.Dispose();
                audioSender.Dispose();

                // a bit naughty but we have designed the codecs to support multiple calls to Dispose, 
                // recreating their resources if Encode/Decode called again
                selectedCodec.Dispose(); 
            }
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
}
