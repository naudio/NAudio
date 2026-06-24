using System.Windows.Forms;

namespace NAudioDemo.SignalGeneratorDemo
{
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
