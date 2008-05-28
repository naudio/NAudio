using System;
using System.Collections.Generic;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    public class AudioClient
    {
        IAudioClient audioClientInterface;

        internal AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }
    }
}
