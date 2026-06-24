using System;
using System.Windows.Forms;

namespace NAudioDemo
{
    public interface INAudioDemoPlugin
    {
        string Name { get; }
        Control CreatePanel();
    }
}
