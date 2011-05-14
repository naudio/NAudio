using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    public interface IInputFileFormatPlugin
    {
        string Name { get; }
        string Extension { get; }
        WaveStream CreateWaveStream(string fileName);
    }
}
