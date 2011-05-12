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
using NAudio.Wave.Compression;
using System.Diagnostics;

namespace NAudioDemo
{
    public partial class NetworkChatPanel : UserControl
    {
        private WaveIn waveIn;
        private UdpClient udpSender;
        private UdpClient udpListener;
        private IWavePlayer waveOut;
        private BufferedWaveProvider waveProvider;
        private INetworkChatCodec codec;
        private volatile bool connected;

        public NetworkChatPanel()
        {
            InitializeComponent();
            PopulateInputDevicesCombo();
            PopulateCodecsCombo();
            this.Disposed += new EventHandler(NetworkChatPanel_Disposed);
        }

        void NetworkChatPanel_Disposed(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void PopulateCodecsCombo()
        {
            var codecs = new INetworkChatCodec[] { 
                new PlainPcmChatCodec(),
                new Gsm610ChatCodec(),
            };
            this.comboBoxCodecs.DisplayMember = "Name";
            foreach(var codec in codecs)
            {
                this.comboBoxCodecs.Items.Add(codec);
            }
            this.comboBoxCodecs.SelectedIndex = 0;
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
                this.codec = (INetworkChatCodec)comboBoxCodecs.SelectedItem;
                Connect(endPoint, inputDeviceNumber,codec);
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
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = inputDeviceNumber;
            waveIn.WaveFormat = codec.RecordFormat;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.StartRecording();
            
            udpSender = new UdpClient();
            udpListener = new UdpClient();
            //endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7080);
            // To allow us to talk to ourselves for test purposes:
            // http://stackoverflow.com/questions/687868/sending-and-receiving-udp-packets-between-two-programs-on-the-same-computer
            //udpSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //udpSender.Client.Bind(endPoint);
            udpListener.Client.Bind(endPoint);

            udpSender.Connect(endPoint);
            
            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(codec.RecordFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            connected = true;
            ListenerThreadState state = new ListenerThreadState() { Codec = codec, EndPoint = endPoint };
            ThreadPool.QueueUserWorkItem(this.ListenerThread, state);
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

                this.codec.Dispose(); // a bit naughty but we have designed the codecs to support multiple calls to Dispose, recreating their resources if Encode/Decode called again
            }
        }

        class ListenerThreadState
        {
            public IPEndPoint EndPoint { get; set; }
            public INetworkChatCodec Codec { get; set; }
        }

        private void ListenerThread(object state)
        {
            ListenerThreadState listenerThreadState = (ListenerThreadState)state;
            IPEndPoint endPoint = listenerThreadState.EndPoint;            
            try
            {
                while (connected)
                {
                    byte[] b = this.udpListener.Receive(ref endPoint);
                    byte[] decoded = listenerThreadState.Codec.Decode(b);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
            }
            catch (SocketException se)
            {
                // usually not a problem - just means we have disconnected
            }
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
            udpSender.Send(encoded, encoded.Length);
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

    interface INetworkChatCodec : IDisposable
    {
        string Name { get; }
        WaveFormat RecordFormat { get; }
        byte[] Encode(byte[] data, int offset, int length);
        byte[] Decode(byte[] data);
    }

    class PlainPcmChatCodec : INetworkChatCodec
    {
        public PlainPcmChatCodec()
        {
            this.RecordFormat = new WaveFormat(8000, 16, 1);
        }
        public string Name { get { return "PCM 8kHz 16 bit uncompressed (128kbps)"; } }
        public WaveFormat RecordFormat { get; private set; }
        public byte[] Encode(byte[] data, int offset, int length) 
        {
            byte[] encoded = new byte[length];
            Array.Copy(data, offset, encoded, 0, length);
            return encoded; 
        }
        public byte[] Decode(byte[] data) { return data; }
        public void Dispose() { }
    }

    abstract class AcmChatCodec : INetworkChatCodec
    {
        private WaveFormat encodeFormat;
        private AcmStream encodeStream;
        private AcmStream decodeStream;
        private int decodeSourceBytesLeftovers;
        private int encodeSourceBytesLeftovers;

        public AcmChatCodec(WaveFormat recordFormat, WaveFormat encodeFormat)
        {
            this.RecordFormat = recordFormat;
            this.encodeFormat = encodeFormat;
        }

        public WaveFormat RecordFormat { get; private set; }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            if (this.encodeStream == null)
            {
                this.encodeStream = new AcmStream(this.RecordFormat, this.encodeFormat);            
            }
            Debug.WriteLine(String.Format("Encoding {0} + {1} bytes",length,encodeSourceBytesLeftovers));                
            return Convert(encodeStream, data, offset, length, ref encodeSourceBytesLeftovers);
        }

        public byte[] Decode(byte[] data)
        {
            if (this.decodeStream == null)
            {
                this.decodeStream = new AcmStream(this.encodeFormat, this.RecordFormat);
            }
            Debug.WriteLine(String.Format("Decoding {0} + {1} bytes", data.Length, decodeSourceBytesLeftovers));
            return Convert(decodeStream, data, 0, data.Length, ref decodeSourceBytesLeftovers);
        }

        private static byte[] Convert(AcmStream conversionStream, byte[] data, int offset, int length, ref int sourceBytesLeftovers)
        {
            int bytesInSourceBuffer = length + sourceBytesLeftovers;
            Array.Copy(data, offset, conversionStream.SourceBuffer, sourceBytesLeftovers, length);
            int sourceBytesConverted;
            int bytesConverted = conversionStream.Convert(bytesInSourceBuffer, out sourceBytesConverted);
            sourceBytesLeftovers = bytesInSourceBuffer - sourceBytesConverted;
            if (sourceBytesLeftovers > 0)
            {
                Debug.WriteLine(String.Format("Asked for {0}, converted {1}", bytesInSourceBuffer, sourceBytesConverted));
                // shift the leftovers down
                Array.Copy(conversionStream.SourceBuffer, sourceBytesConverted, conversionStream.SourceBuffer, 0, sourceBytesLeftovers);
            }
            byte[] encoded = new byte[bytesConverted];
            Array.Copy(conversionStream.DestBuffer, 0, encoded, 0, bytesConverted);
            return encoded;
        }

        public abstract string Name { get; }

        public void Dispose()
        {
            if (encodeStream != null) 
            {
                encodeStream.Dispose();
                encodeStream = null;
            }
            if (decodeStream != null)
            {
                decodeStream.Dispose();
                decodeStream = null;
            }
        }
    }

    class Gsm610ChatCodec : AcmChatCodec
    {
        public Gsm610ChatCodec()
            : base(new WaveFormat(8000, 16, 1),new Gsm610WaveFormat())
        {
        }

        public override string Name { get { return "GSM 6.10 (13kbps)"; } }       
    }
}
