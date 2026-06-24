namespace NAudioWpfDemo.AudioPlaybackDemo;

internal class SpectrumAnalyzerVisualization : IVisualizationPlugin
{
    private readonly SpectrumAnalyser spectrumAnalyser = new();

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

    public void OnSourceChanged(int sampleRate)
    {
        spectrumAnalyser.SampleRate = sampleRate;
    }
}
