using NAudioDemo.FadeInOutDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioDemo.DeviceTopology
{
    class DeviceTopologyPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Device Topology"; }
        }

        public System.Windows.Forms.Control CreatePanel()
        {
            return new DeviceTopologyPanel();
        }
    }
}
