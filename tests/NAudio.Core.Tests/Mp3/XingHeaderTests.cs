using System.IO;
using System.Text;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.Mp3;

[TestFixture]
[Category("UnitTest")]
public class XingHeaderTests
{
    // MPEG-2 Layer III, 8 kbps, 24 kHz, no CRC: the smallest frame there is, 24 bytes
    private static Mp3Frame TinyFrame(bool mono, string tag, uint flags)
    {
        var frame = new byte[24];
        frame[0] = 0xFF;
        frame[1] = 0xF3;
        frame[2] = 0x14;
        frame[3] = mono ? (byte)0xC0 : (byte)0x00;
        int offset = mono ? 13 : 21;
        var tagBytes = Encoding.ASCII.GetBytes(tag);
        for (int i = 0; i < tagBytes.Length && offset + i < frame.Length; i++) frame[offset + i] = tagBytes[i];
        if (offset + 8 <= frame.Length)
        {
            frame[offset + 4] = (byte)(flags >> 24);
            frame[offset + 5] = (byte)(flags >> 16);
            frame[offset + 6] = (byte)(flags >> 8);
            frame[offset + 7] = (byte)flags;
        }
        var loaded = Mp3Frame.LoadFromStream(new MemoryStream(frame));
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.FrameLength, Is.EqualTo(24));
        return loaded;
    }

    [Test]
    public void FrameTooSmallToHoldATagHasNoXingHeader()
    {
        // the stereo side-info offset (21) leaves no room for the tag in a 24-byte frame
        var frame = TinyFrame(mono: false, "Xing", 0);
        Assert.That(XingHeader.LoadXingHeader(frame), Is.Null);
    }

    [Test]
    public void TagWhoseFlaggedFieldsDoNotFitIsIgnored()
    {
        // frames + bytes + TOC + VBR scale: 112 bytes of fields in an 24-byte frame
        var frame = TinyFrame(mono: true, "Xing", 0xF);
        Assert.That(XingHeader.LoadXingHeader(frame), Is.Null);
    }

    [Test]
    public void TagWithFieldsThatFitIsRead()
    {
        var frame = TinyFrame(mono: true, "Info", 0);
        Assert.That(XingHeader.LoadXingHeader(frame), Is.Not.Null);
    }
}
