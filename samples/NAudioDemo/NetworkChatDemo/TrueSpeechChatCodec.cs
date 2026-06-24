using System;
using System.Linq;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    /// <summary>
    /// DSP Group TrueSpeech codec, using ACM
    /// n.b. Windows XP came with a TrueSpeech codec built in
    /// - looks like Windows 7 doesn't
    /// </summary>
    class TrueSpeechChatCodec : AcmChatCodec
    {
        public TrueSpeechChatCodec()
            : base(new WaveFormat(8000, 16, 1), new TrueSpeechWaveFormat())
        {
        }
    
        public override string Name => "DSP Group TrueSpeech";
    }
}
