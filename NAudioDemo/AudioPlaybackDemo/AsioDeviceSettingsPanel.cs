using System;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    public partial class AsioDeviceSettingsPanel : UserControl
    {
        public AsioDeviceSettingsPanel()
        {
            InitializeComponent();
            foreach (var driverName in AsioDevice.GetDriverNames())
            {
                comboBoxAsioDriver.Items.Add(driverName);
            }
            if (comboBoxAsioDriver.Items.Count > 0)
            {
                comboBoxAsioDriver.SelectedIndex = 0;
            }
        }

        public string SelectedDeviceName => (string)comboBoxAsioDriver.SelectedItem;

        public int OutputChannelOffset
        {
            get => int.TryParse(textBoxChannelOffset.Text, out int offset) ? offset : 0;
        }

        private void OnButtonControlPanelClick(object sender, EventArgs args)
        {
            try
            {
                using var device = AsioDevice.Open(SelectedDeviceName);
                device.ShowControlPanel();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
