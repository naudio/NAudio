using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.ComponentModel.Composition;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export(typeof(IInputFileFormatPlugin))]
    class WaveInputFilePlugin : IInputFileFormatPlugin
    {
        public string Name
        {
            get { return "WAV file"; }
        }

        public string Extension
        {
            get { return ".wav"; }
        }

        public WaveStream CreateWaveStream(string fileName)
        {
            WaveStream readerStream = new WaveFileReader(fileName);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }
            return readerStream;
        }
    }
}
