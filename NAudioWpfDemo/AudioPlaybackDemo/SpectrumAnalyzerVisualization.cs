using System;
using System.Linq;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    class SpectrumAnalyzerVisualization : IVisualizationPlugin
    {
        private readonly SpectrumAnalyser spectrumAnalyser = new SpectrumAnalyser();

        public string Name => "Spectrum Analyser";

        public object Content => spectrumAnalyser;

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
