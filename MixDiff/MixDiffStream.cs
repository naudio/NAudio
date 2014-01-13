using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace MarkHeath.AudioUtils
{
    public class MixDiffStream : WaveStream
    {
        WaveOffsetStream offsetStream;
        WaveChannel32 channelSteam;
        bool muted;
        float volume;

        public MixDiffStream(string fileName)
        {
            WaveFileReader reader = new WaveFileReader(fileName);
            offsetStream = new WaveOffsetStream(reader);
            channelSteam = new WaveChannel32(offsetStream);
            muted = false;
            volume = 1.0f;
        }

        public override int BlockAlign
        {
            get
            {
                return channelSteam.BlockAlign;
            }
        }

        public override WaveFormat WaveFormat
        {
            get { return channelSteam.WaveFormat; }
        }

        public override long Length
        {
            get { return channelSteam.Length; }
        }

        public override long Position
        {
            get
            {
                return channelSteam.Position;
            }
            set
            {
                channelSteam.Position = value;
            }
        }

        public bool Mute
        {
            get
            {
                return muted;
            }
            set
            {
                muted = value;
                if (muted)
                {
                    channelSteam.Volume = 0.0f;
                }
                else
                {
                    // reset the volume                
                    Volume = Volume;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return channelSteam.Read(buffer, offset, count);
        }

        public override bool HasData(int count)
        {
            return channelSteam.HasData(count);
        }

        public float Volume
        {
            get 
            { 
                return volume; 
            }
            set 
            {
                volume = value;
                if (!Mute)
                {
                    channelSteam.Volume = volume;
                }
            }
        }

        public TimeSpan PreDelay
        {
            get { return offsetStream.StartTime; }
            set { offsetStream.StartTime = value; }
        }

        public TimeSpan Offset
        {
            get { return offsetStream.SourceOffset; }
            set { offsetStream.SourceOffset = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (channelSteam != null)
            {
                channelSteam.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
