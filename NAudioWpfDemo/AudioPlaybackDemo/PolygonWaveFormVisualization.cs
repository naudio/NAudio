using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    [Export(typeof(IVisualizationPlugin))]
    class PolygonWaveFormVisualization : IVisualizationPlugin
    {
        private PolygonWaveFormControl polygonWaveFormControl = new PolygonWaveFormControl();

        public string Name
        {
            get { return "Polygon WaveForm Visualization"; }
        }

        public object Content
        {
            get { return polygonWaveFormControl; }
        }


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
