using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudioDemo.DeviceTopology
{
    public partial class DeviceTopologyPanel : UserControl
    {
        class AudioVolumeWrapper
        {
            public uint Channel { get; set; }
            public AudioVolumeLevel AudioVolumeLevel { get; set; }
        }

        public DeviceTopologyPanel()
        {
            InitializeComponent();

            foreach (var device in GetCaptureDevices())
            {
                cbDevices.Items.Add(device);
            }

            foreach (var device in GetRenderDevices())
            {
                cbDevices.Items.Add(device);
            }

            if (cbDevices.Items.Count > 0)
            {
                cbDevices.SelectedIndex = 0;
            }
        }

        public IEnumerable<MMDevice> GetCaptureDevices()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var audioEndPoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

                foreach (var device in audioEndPoints)
                {
                    yield return device;
                }
            }
        }

        public IEnumerable<MMDevice> GetRenderDevices()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var audioEndPoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in audioEndPoints)
                {
                    yield return device;
                }
            }
        }

        private void cbDevices_SelectedValueChanged(object sender, EventArgs e)
        {
            var device = (MMDevice)cbDevices.SelectedItem;
            var topology = device.DeviceTopology;
            var connector = topology.GetConnector(0);

            var deviceConnector = connector.ConnectedTo;
            tbTopology.Text = "";
            _superMixBox = null;
            WalkParts(deviceConnector.Part, device.DataFlow == DataFlow.Render, "");
        }

        private bool HasVolumeOrMuteParts(Part part)
        {
            if (part.Name == "Volume" || part.Name == "Mute")
            {
                return true;
            }

            for (uint i = 0; i < part.PartsIncoming.Count; i++)
            {
                if (HasVolumeOrMuteParts(part.PartsIncoming[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void WalkParts(Part part, bool isRenderDevice, string indent)
        {
            var audioVolumeLevel = part.AudioVolumeLevel;
            var audioMute = part.AudioMute;
            var jackDescription = part.JackDescription;

            var s = "\r\n";
            if (audioVolumeLevel != null)
            {
                s = $" (audio volume level)\r\n";
                for (uint i = 0; i < audioVolumeLevel.ChannelCount; i++)
                {
                    audioVolumeLevel.GetLevelRange(i, out var minLevelDb, out var maxLevelDb, out var stepping);
                    var volume = audioVolumeLevel.GetLevel(i);
                    s += $"{indent}Channel: {i}, Min {minLevelDb:0.##} dB, Max {maxLevelDb:0.##} dB, stepping {stepping:0.##} dB: {volume:0.##} dB\r\n";
                }
            }
            else if (audioMute != null)
            {
                s = $" (audio mute. Muted: {audioMute.IsMuted})\r\n";
            } if (jackDescription != null)
            {
                s = $" (Jacks: {jackDescription.Count})\r\n";
            }

            tbTopology.AppendText($"{indent}{part.PartType}: {part.Name}{s}");
            if (part.Name.ToLowerInvariant() == "supermix")
            {
                if (isRenderDevice && HasVolumeOrMuteParts(part))
                {
                    _superMixBox = new GroupBox
                    {
                        Text = $"Supermix",
                        Dock = DockStyle.Top
                    };
                }
            }

            var parts = isRenderDevice ? part.PartsIncoming : part.PartsOutgoing;
            for (uint i = 0; i < parts.Count; i++)
            {
                var outgoingPart = parts[i];
                WalkParts(outgoingPart, isRenderDevice, indent + "  ");
            }
        }

        private void Tb_ValueChanged(object sender, EventArgs e)
        {
            var trackBar = (TrackBar)sender;
            var wrapper = (AudioVolumeWrapper)trackBar.Tag;

            wrapper.AudioVolumeLevel.GetLevelRange(wrapper.Channel, out var minLevelDb, out float maxLevelDb, out float _);
            wrapper.AudioVolumeLevel.SetLevel(wrapper.Channel, (float)LinearToDecibels(trackBar.Value, minLevelDb, maxLevelDb));
        }

        private static double LinearToDecibels(double linearValue, double minDb, double maxDb)
        {
            // Calculate the minimum and maximum values in linear scale
            double minLinearValue = Math.Pow(10, minDb / 10.0);
            double maxLinearValue = Math.Pow(10, maxDb / 10.0);

            // Convert the percentage to a linear value within the given range
            double linearRangeValue = (linearValue / 100.0) * (maxLinearValue - minLinearValue) + minLinearValue;

            // Convert the linear value to decibels
            return 10.0 * Math.Log10(linearRangeValue);
        }

        private static double DecibelsToLinear(double valueInDb, double minDb, double maxDb)
        {
            // Convert the value from decibels to linear scale
            double linearValue = Math.Pow(10, valueInDb / 10.0);

            // Calculate the minimum and maximum values in linear scale
            double minLinearValue = Math.Pow(10, minDb / 10.0);
            double maxLinearValue = Math.Pow(10, maxDb / 10.0);

            // Convert the linear value to a percentage within the given range
            double percentage = (linearValue - minLinearValue) / (maxLinearValue - minLinearValue) * 100.0;

            // Ensure the percentage is within the valid range from 0 to 100
            return Math.Max(Math.Min(percentage, 100.0), 0.0);

        }

        private GroupBox _superMixBox;
    }
}
