using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.ComponentModel.Composition;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export(typeof(IInputFileFormatPlugin))]
    class Mp3InputFilePlugin : IInputFileFormatPlugin
    {
        public string Name
        {
            get { return "MP3 File"; }
        }

        public string Extension
        {
            get { return ".mp3"; }
        }

        public WaveStream CreateWaveStream(string fileName)
        {
            return new Mp3FileReader(fileName);
        }
    }
}
