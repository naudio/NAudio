using System;
using System.Linq;
using System.Windows.Forms;
using NAudioDemo.Utils;

namespace NAudioDemo
{
    public sealed partial class MainForm : Form
    {
        private INAudioDemoPlugin currentPlugin;

        public MainForm()
        {
            // use reflection to find all the demos
            var demos = ReflectionHelper.CreateAllInstancesOf<INAudioDemoPlugin>().OrderBy(d => d.Name);

            InitializeComponent();
            listBoxDemos.DisplayMember = "Name";
            foreach (var demo in demos)
            {
                listBoxDemos.Items.Add(demo);
            }

            Text += ((System.Runtime.InteropServices.Marshal.SizeOf(IntPtr.Zero) == 8) ? " (x64)" : " (x86)");
        }


        private void OnLoadDemoClick(object sender, EventArgs e)
        {
            var plugin = (INAudioDemoPlugin)listBoxDemos.SelectedItem;
            if (plugin == currentPlugin) return;
            currentPlugin = plugin;
            DisposeCurrentDemo();
            var control = plugin.CreatePanel();
            control.Dock = DockStyle.Fill;
            panelDemo.Controls.Add(control);
        }

        private void DisposeCurrentDemo()
        {
            if (panelDemo.Controls.Count <= 0) return;
            panelDemo.Controls[0].Dispose();
            panelDemo.Controls.Clear();
            GC.Collect();
        }

        private void OnListBoxDemosDoubleClick(object sender, EventArgs e)
        {
            OnLoadDemoClick(sender, e);
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeCurrentDemo();
        }
    }
}