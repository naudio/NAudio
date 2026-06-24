using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudioDemo.Utils;

namespace NAudioDemo.AudioPlaybackDemo
{
    public partial class WasapiOutSettingsPanel : UserControl
    {
        private readonly MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        private readonly DeviceChangeNotifier deviceChangeNotifier;

        public WasapiOutSettingsPanel()
        {
            InitializeComponent();
            InitialiseWasapiControls();
            deviceChangeNotifier = new DeviceChangeNotifier(enumerator);
            deviceChangeNotifier.DevicesChanged += InitialiseWasapiControls;
            Disposed += (_, _) => { deviceChangeNotifier.Dispose(); enumerator.Dispose(); };
        }

        class WasapiDeviceComboItem
        {
            public string Description { get; set; }
            public MMDevice Device { get; set; }
        }

        private void InitialiseWasapiControls()
        {
            var endPoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var comboItems = new List<WasapiDeviceComboItem>();
            string defaultId = null;
            try { defaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID; }
            catch { /* may legitimately have no default */ }

            // Preserve current selection across refreshes when possible.
            var prevSelectedId = (comboBoxWaspai.SelectedValue as MMDevice)?.ID;

            foreach (var endPoint in endPoints)
            {
                comboItems.Add(new WasapiDeviceComboItem
                {
                    Description = string.Format("{0} ({1})", endPoint.FriendlyName, endPoint.DeviceFriendlyName),
                    Device = endPoint,
                });
            }
            comboBoxWaspai.DisplayMember = "Description";
            comboBoxWaspai.ValueMember = "Device";
            comboBoxWaspai.DataSource = comboItems;

            int idx = -1;
            if (prevSelectedId != null) idx = comboItems.FindIndex(ci => ci.Device.ID == prevSelectedId);
            if (idx == -1 && defaultId != null) idx = comboItems.FindIndex(ci => ci.Device.ID == defaultId);
            if (idx != -1) comboBoxWaspai.SelectedIndex = idx;
        }

        public MMDevice SelectedDevice { get { return (MMDevice)comboBoxWaspai.SelectedValue; } }

        public AudioClientShareMode ShareMode
        {
            get
            {
                return checkBoxWasapiExclusiveMode.Checked ?
                    AudioClientShareMode.Exclusive :
                    AudioClientShareMode.Shared;
            }
        }

        public bool UseEventCallback { get { return checkBoxWasapiEventCallback.Checked; } }
    }
}
