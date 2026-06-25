namespace NAudioDemo.DeviceTopology;

internal class DeviceTopologyPlugin : INAudioDemoPlugin
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
