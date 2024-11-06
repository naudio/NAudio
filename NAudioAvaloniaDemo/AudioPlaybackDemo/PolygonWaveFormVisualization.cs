using NAudioAvaloniaDemo.VisualizationControls;

namespace NAudioAvaloniaDemo.AudioPlaybackDemo
{
    class PolygonWaveFormVisualization : IVisualizationPlugin
    {
        private readonly PolygonWaveFormControl polygonWaveFormControl = new PolygonWaveFormControl();

        public string Name => "Polygon WaveForm";

        public object Content => polygonWaveFormControl;

        public void OnMaxCalculated(float min, float max)
        {
            polygonWaveFormControl.AddValue(max, min);
        }

        public void OnFftCalculated(NAudio.Dsp.Complex[] result)
        {
            // nothing to do
        }
    }
}
