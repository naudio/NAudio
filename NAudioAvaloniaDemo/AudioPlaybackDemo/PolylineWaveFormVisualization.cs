using NAudioAvaloniaDemo.VisualizationControls;

namespace NAudioAvaloniaDemo.AudioPlaybackDemo
{
    class PolylineWaveFormVisualization : IVisualizationPlugin
    {
        private readonly PolylineWaveFormControl polylineWaveFormControl = new PolylineWaveFormControl();

        public string Name => "Polyline WaveForm Visualization";

        public object Content => polylineWaveFormControl;

        public void OnMaxCalculated(float min, float max)
        {
            polylineWaveFormControl.AddValue(max, min);
        }

        public void OnFftCalculated(NAudio.Dsp.Complex[] result)
        {
            // nothing to do
        }
    }
}
