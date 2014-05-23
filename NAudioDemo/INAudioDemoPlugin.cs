using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace NAudioDemo
{
    public interface INAudioDemoPlugin
    {
        string Name { get; }
        Control CreatePanel();
    }
}
