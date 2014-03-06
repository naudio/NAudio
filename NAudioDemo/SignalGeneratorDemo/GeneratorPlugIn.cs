using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace NAudioDemo.Generator
{
    [Export(typeof (INAudioDemoPlugin))]
    internal class GeneratorPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Signal Generator"; }
        }

        public Control CreatePanel()
        {
            return new GeneratorPanel();
        }
    }
}
