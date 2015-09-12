using System;
using System.Linq;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    class MicrosoftAdpcmChatCodec : AcmChatCodec
    {
        public MicrosoftAdpcmChatCodec()
            : base(new WaveFormat(8000, 16, 1), new AdpcmWaveFormat(8000,1))
        {
        }

        public override string Name { get { return "Microsoft ADPCM"; } }
    }
}
