using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    public partial class WaveOutSettingsPanel : UserControl
    {
        public WaveOutSettingsPanel()
        {
            InitializeComponent();
            InitialiseDeviceCombo();
            InitialiseStrategyCombo();
        }

        internal class CallbackComboItem
        {
            public CallbackComboItem(string text, WaveCallbackStrategy strategy)
            {
                Text = text;
                Strategy = strategy;
            }
            public string Text { get; private set; }
            public WaveCallbackStrategy Strategy { get; }
        }

        private void InitialiseDeviceCombo()
        {
            if (WaveOut.DeviceCount <= 0) return;
            for (var deviceId = -1; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var capabilities = WaveOut.GetCapabilities(deviceId);
                comboBoxWaveOutDevice.Items.Add($"Device {deviceId} ({capabilities.ProductName})");
            }
            comboBoxWaveOutDevice.SelectedIndex = 0;
        }

        private void InitialiseStrategyCombo()
        {
            comboBoxCallback.DisplayMember = "Text";
            comboBoxCallback.ValueMember = "Strategy";
            comboBoxCallback.Items.Add(new CallbackComboItem("Window", WaveCallbackStrategy.NewWindow));
            comboBoxCallback.Items.Add(new CallbackComboItem("Function", WaveCallbackStrategy.FunctionCallback));
            comboBoxCallback.Items.Add(new CallbackComboItem("Event", WaveCallbackStrategy.Event));
            comboBoxCallback.SelectedIndex = 0;
        }

        public int SelectedDeviceNumber => comboBoxWaveOutDevice.SelectedIndex;

        public WaveCallbackStrategy CallbackStrategy => ((CallbackComboItem)comboBoxCallback.SelectedItem).Strategy;
    }
}
