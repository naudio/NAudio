using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.ComponentModel.Composition;

namespace NAudioDemo.NetworkChatDemo
{
    [Export(typeof(INetworkChatCodec))]
    class TrueSpeechChatCodec : AcmChatCodec
    {
        public TrueSpeechChatCodec()
            : base(new WaveFormat(8000, 16, 1), new TrueSpeechWaveFormat())
        {
        }
    

        public override string Name
        {
            get { return "DSP Group TrueSpeech"; }
        }

    }
}
