using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    [Export(typeof(IVisualizationPlugin))]
    class SpectrumAnalyzerVisualization : IVisualizationPlugin
    {
        private SpectrumAnalyser spectrumAnalyser = new SpectrumAnalyser();

        public string Name
        {
            get { return "Spectrum Analyser"; }
        }

        public object Content
        {
            get { return spectrumAnalyser; }
        }


        public void OnMaxCalculated(float min, float max)
        {
            // nothing to do
        }

        public void OnFftCalculated(NAudio.Dsp.Complex[] result)
        {
            spectrumAnalyser.Update(result);
        }
    }
}
