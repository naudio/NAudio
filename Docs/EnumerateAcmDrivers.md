# Enumerate ACM Drivers

ACM drivers are the old Windows API for dealing with compressed audio that predates Media Foundation. In one sense this means that this is no longer very important, but sometimes you find that some codecs are more readily available as ACM codecs instead of Media Foundation Transforms. 

The class in NAudio that makes use of ACM codecs is `WaveFormatConversionStream`. When you construct one you provide it with a source and a target `WaveFormat`. This will be either going from compressed audio to PCM (this is a decoder) or from PCM to compressed (this is an encoder). Its important to not that you can't just pick two random `WaveFormat` definitions and expect a conversion to be possible. You can only perform the supported transforms.

That's why it's really useful to be able to enumerate the ACM codecs installed on your system. You can do that with `AcmDriver.EnumerateAcmDrivers`. Then you explore the `FormatTags` for each driver, and from there ask for each format matching that tag with `driver.GetFormats`.

It is a little complex, but the information you get from doing this is invaluable in helping you to work out exactly what `WaveFormat` you need to use to successfully use a codec. 

This code sample enumerates through all ACM drivers and prints out details of their formats.

```c#
foreach (var driver in AcmDriver.EnumerateAcmDrivers())
{
    StringBuilder builder = new StringBuilder();
    builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
    builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
    builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
    driver.Open();
	builder.AppendFormat("FormatTags:\r\n");
    foreach (AcmFormatTag formatTag in driver.FormatTags)
    {
        builder.AppendFormat("===========================================\r\n");
        builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
        builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
        builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
        builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
        builder.AppendFormat("   Formats:\r\n");
        foreach (AcmFormat format in driver.GetFormats(formatTag))
        {
            builder.AppendFormat("   ===========================================\r\n");
            builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
            builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
            builder.AppendFormat("      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                (format.WaveFormat.AverageBytesPerSecond * 8) / 1000.0,
                format.WaveFormat.ExtraSize);
            if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
            {
                WaveFormatExtraData wfed = (WaveFormatExtraData)format.WaveFormat;
                builder.Append("      Extra Bytes:\r\n      ");
                for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                {
                    builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                }
                builder.Append("\r\n");
            }
        }
    }
    driver.Close();
    Console.WriteLine(builder.ToString());
}
```


The output will be quite verbose (especially if you've installed some additional codecs on your system.) Here's a snippet of the output from the GSM codec:


```
Long Name: Microsoft GSM 6.10 Audio CODEC
Short Name: Microsoft GSM 6.10
Driver ID: 48141232
FormatTags:
===========================================
Format Tag 0: PCM
   Standard Format Count: 8
   Support Flags: Codec
   Format Tag: Pcm, Format Size: 16
   Formats:
   ===========================================
   Format 0: 8.000 kHz, 8 Bit, Mono
      FormatTag: Pcm, Support Flags: Codec
      WaveFormat: Pcm 8000Hz Channels: 1 Bits: 8 Block Align: 1, AverageBytesPerSecond: 8000 (64.0 kbps), Extra Size: 0
   ===========================================
   Format 1: 8.000 kHz, 16 Bit, Mono
      FormatTag: Pcm, Support Flags: Codec
      WaveFormat: Pcm 8000Hz Channels: 1 Bits: 16 Block Align: 2, AverageBytesPerSecond: 16000 (128.0 kbps), Extra Size: 0
   ===========================================
   Format 2: 11.025 kHz, 8 Bit, Mono
      FormatTag: Pcm, Support Flags: Codec
      WaveFormat: Pcm 11025Hz Channels: 1 Bits: 8 Block Align: 1, AverageBytesPerSecond: 11025 (88.2 kbps), Extra Size: 0
```

And here's an example showing a non-PCM format. Here we can see that for `DviAdpcm`, the `WaveFormat` structure needs two extra bytes with values 0xF9 and 0x01:

```
   ===========================================
   Format 1: 8.000 kHz, 4 Bit, Stereo
      FormatTag: DviAdpcm, Support Flags: Codec
      WaveFormat: DviAdpcm 8000Hz Channels: 2 Bits: 4 Block Align: 512, AverageBytesPerSecond: 8110 (64.9 kbps), Extra Size: 2
      Extra Bytes:
      F9 01 
```
