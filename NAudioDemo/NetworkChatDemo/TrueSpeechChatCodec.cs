using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    class TrueSpeechChatCodec : AcmChatCodec
    {
        public TrueSpeechChatCodec()
            : base(new WaveFormat(8000, 16, 1), new TrueSpeechWaveFormat())
        {
        }
    

        public override string Name
        {
            get { return "DSP Group TrueSpeech (8.5kbps)"; }
        }

    }
}
