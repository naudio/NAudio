using System;
using System.Linq;
using NAudio.Dsp;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    interface IVisualizationPlugin
    {
        string Name { get; }
        object Content { get; }

        // n.b. not great design, need to refactor so visualizations can attach to the playback graph and measure just what they need
        void OnMaxCalculated(float min, float max);
        void OnFftCalculated(Complex[] result);
    }
}
