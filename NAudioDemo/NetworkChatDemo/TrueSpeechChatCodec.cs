using System;
using System.Linq;
using NAudio.Wave;
using System.ComponentModel.Composition;

namespace NAudioDemo.NetworkChatDemo
{
    /// <summary>
    /// DSP Group TrueSpeech codec, using ACM
    /// n.b. Windows XP came with a TrueSpeech codec built in
    /// - looks like Windows 7 doesn't
    /// </summary>
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
