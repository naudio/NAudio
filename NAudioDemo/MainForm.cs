using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Composition;

namespace NAudioDemo
{
    [Export]
    public partial class MainForm : Form
    {
        [ImportingConstructor]
        public MainForm([ImportMany] IEnumerable<INAudioDemoPlugin> demos)
        {
            InitializeComponent();
            listBoxDemos.DisplayMember = "Name";
            foreach (var demo in demos)
            {
                listBoxDemos.Items.Add(demo);
            }
        }

        private INAudioDemoPlugin currentPlugin;

        private void buttonLoadDemo_Click(object sender, EventArgs e)
        {
            var plugin = (INAudioDemoPlugin)listBoxDemos.SelectedItem;
            if (plugin != currentPlugin)
            {
                this.currentPlugin = plugin;
                if (panelDemo.Controls.Count > 0)
                {
                    panelDemo.Controls[0].Dispose();
                    panelDemo.Controls.Clear();
                    GC.Collect();
                }
                var control = plugin.CreatePanel();
                control.Dock = DockStyle.Fill;
                panelDemo.Controls.Add(control);
            }
        }

        private void listBoxDemos_DoubleClick(object sender, EventArgs e)
        {
            buttonLoadDemo_Click(sender, e);
        }
    }
}