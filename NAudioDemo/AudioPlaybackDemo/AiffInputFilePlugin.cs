using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export(typeof(IInputFileFormatPlugin))]
    class AiffInputFilePlugin : IInputFileFormatPlugin
    {
        public string Name
        {
            get { return "AIFF File"; }
        }

        public string Extension
        {
            get { return ".aiff"; }
        }

        public WaveStream CreateWaveStream(string fileName)
        {
            return new AiffFileReader(fileName);
        }
    }
}
