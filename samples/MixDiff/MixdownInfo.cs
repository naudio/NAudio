using System;
using NAudio.Utils;

namespace MarkHeath.AudioUtils;

public class MixdownInfo
{
    private readonly string fileName;
    private string letter;
    private readonly MixDiffStream stream;
    private int offsetMilliseconds;
    private int delayMilliseconds;
    private int volumeDecibels;

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
            stream.Volume = (float)Decibels.DecibelsToLinear(volumeDecibels);
        }
    }
}
