using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.ComponentModel.Composition;

namespace NAudioDemo
{
    public partial class NetworkChatPanel : UserControl
    {
        private WaveIn waveIn;
        private UdpClient udpSender;
        private UdpClient udpListener;
        private IWavePlayer waveOut;
        private BufferedWaveProvider waveProvider;
        private WaveFormat waveFormat;
        private volatile bool connected;

        public NetworkChatPanel()
        {
            InitializeComponent();
            PopulateInputDevicesCombo();
            this.Disposed += new EventHandler(NetworkChatPanel_Disposed);
        }

        void NetworkChatPanel_Disposed(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void PopulateInputDevicesCombo()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                this.comboBoxInputDevices.Items.Add(capabilities.ProductName);
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
                Connect(endPoint, inputDeviceNumber);
                buttonStartStreaming.Text = "Disconnect";
            }
            else
            {
                Disconnect();
                buttonStartStreaming.Text = "Connect";
            }
        }

        private void Connect(IPEndPoint endPoint, int inputDeviceNumber)
        {
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = inputDeviceNumber;
            this.waveFormat = new WaveFormat(8000, 16, 1);
            waveIn.WaveFormat = waveFormat;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.StartRecording();
            
            udpSender = new UdpClient();
            udpListener = new UdpClient();
            //endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7080);
            // To allow us to talk to ourselves for test purposes:
            // http://stackoverflow.com/questions/687868/sending-and-receiving-udp-packets-between-two-programs-on-the-same-computer
            udpSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpSender.Client.Bind(endPoint);
            udpListener.Client.Bind(endPoint);

            udpSender.Connect(endPoint);

            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(waveFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            connected = true;
            ThreadPool.QueueUserWorkItem(this.ListenerThread, endPoint);
        }

        private void Disconnect()
        {
            if (connected)
            { 
                connected = false;
                waveIn.DataAvailable -= waveIn_DataAvailable;
                waveIn.StopRecording();
                waveOut.Stop();

                udpSender.Close();
                udpListener.Close();
                waveIn.Dispose();
                waveOut.Dispose();
            }
        }

        private void ListenerThread(object state)
        {
            IPEndPoint endPoint = (IPEndPoint)state;
            try
            {
                while (connected)
                {
                    byte[] b = this.udpListener.Receive(ref endPoint);
                    waveProvider.AddSamples(b, 0, b.Length);
                }
            }
            catch (SocketException se)
            {
                // usually not a problem - just means we have disconnected
            }
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {           
            udpSender.Send(e.Buffer, e.BytesRecorded);
        }
    }

    [Export(typeof(INAudioDemoPlugin))]
    public class NetworkChatPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Network Chat"; }
        }

        public Control CreatePanel()
        {
            return new NetworkChatPanel();
        }
    }
}
