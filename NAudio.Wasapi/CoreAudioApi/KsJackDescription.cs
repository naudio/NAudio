using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    public class KsJackDescription
    {
        private readonly IKsJackDescription ksJackDescriptionInterface;

        internal KsJackDescription(IKsJackDescription ksJackDescription)
        {
            ksJackDescriptionInterface = ksJackDescription;
        }

        public uint Count
        {
            get
            {
                ksJackDescriptionInterface.GetJackCount(out var result);
                return result;
            }
        }

        public string this[uint index]
        {
            get
            {
                ksJackDescriptionInterface.GetJackDescription(index, out var result);
                return result;
            }
        }
    }
}
