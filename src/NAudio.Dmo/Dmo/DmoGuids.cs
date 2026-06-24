using System;

namespace NAudio.Dmo;

/// <summary>
/// DMO Guids for use with DMOEnum
/// dmoreg.h
/// </summary>
internal static class DmoGuids
{
    public static readonly Guid DMOCATEGORY_AUDIO_DECODER = new("57f2db8b-e6bb-4513-9d43-dcd2a6593125");
    public static readonly Guid DMOCATEGORY_AUDIO_ENCODER = new("33D9A761-90C8-11d0-BD43-00A0C911CE86");
    public static readonly Guid DMOCATEGORY_VIDEO_DECODER = new("4a69b442-28be-4991-969c-b500adf5d8a8");
    public static readonly Guid DMOCATEGORY_VIDEO_ENCODER = new("33D9A760-90C8-11d0-BD43-00A0C911CE86");
    public static readonly Guid DMOCATEGORY_AUDIO_EFFECT = new("f3602b3f-0592-48df-a4cd-674721e7ebeb");
    public static readonly Guid DMOCATEGORY_VIDEO_EFFECT = new("d990ee14-776c-4723-be46-3da2f56f10b9");
    public static readonly Guid DMOCATEGORY_AUDIO_CAPTURE_EFFECT = new("f665aaba-3e09-4920-aa5f-219811148f09");
}

internal static class DmoMediaTypeGuids
{
    public static readonly Guid FORMAT_None = new("0F6417D6-C318-11D0-A43F-00A0C9223196");
    public static readonly Guid FORMAT_VideoInfo = new("05589f80-c356-11ce-bf01-00aa0055595a");
    public static readonly Guid FORMAT_VideoInfo2 = new("F72A76A0-EB0A-11d0-ACE4-0000C0CC16BA");
    public static readonly Guid FORMAT_WaveFormatEx = new("05589f81-c356-11ce-bf01-00aa0055595a");
    public static readonly Guid FORMAT_MPEGVideo = new("05589f82-c356-11ce-bf01-00aa0055595a");
    public static readonly Guid FORMAT_MPEGStreams = new("05589f83-c356-11ce-bf01-00aa0055595a");
    public static readonly Guid FORMAT_DvInfo = new("05589f84-c356-11ce-bf01-00aa0055595a");
    public static readonly Guid FORMAT_525WSS = new("C7ECF04D-4582-4869-9ABB-BFB523B62EDF");
}
