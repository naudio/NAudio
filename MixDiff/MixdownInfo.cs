using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using NAudio.Utils;

namespace MarkHeath.AudioUtils
{
    public class MixdownInfo
    {
        string fileName;
        string letter;
        MixDiffStream stream;
        int offsetMilliseconds;
        int delayMilliseconds;
        int volumeDecibels;

        public MixdownInfo(string fileName)
        {
            this.fileName = fileName;
            this.stream = new MixDiffStream(fileName);            
        }

        public string FileName
        {
            get { return fileName; }
        }

        public string Letter
        {
            get { return letter; }
            set { letter = value; }
        }

        public MixDiffStream Stream
        {
            get { return stream; }
        }

        public int OffsetMilliseconds
        {
            get { return offsetMilliseconds; }
            set 
            { 
                offsetMilliseconds = value;
                stream.Offset = TimeSpan.FromMilliseconds(offsetMilliseconds);
            }
        }

        public int DelayMilliseconds
        {
            get { return delayMilliseconds; }
            set 
            { 
                delayMilliseconds = value;
                stream.PreDelay = TimeSpan.FromMilliseconds(delayMilliseconds);
            }
        }

        public int VolumeDecibels
        {
            get 
            { 
                return volumeDecibels; 
            }
            set 
            { 
                volumeDecibels = value;
                stream.Volume = (float) Decibels.DecibelsToLinear(volumeDecibels);
            }
        }
    }
}
