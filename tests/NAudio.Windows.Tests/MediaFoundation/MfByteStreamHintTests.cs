using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.MediaFoundation;

/// <summary>
/// Tests for the byte-stream format hints that <see cref="StreamMediaFoundationReader"/>
/// forwards to <see cref="MfByteStreamFromStream"/>. These assert that the correct
/// <c>MF_BYTESTREAM_CONTENT_TYPE</c> / <c>MF_BYTESTREAM_ORIGIN_NAME</c> attributes are set,
/// the hint precedence is honoured, and the content sniffer still runs as the fallback -
/// none of which needs an installed codec, audio hardware, or test files, only the Media
/// Foundation platform (mfplat). Hence these are not marked IntegrationTest so they run in
/// the headless CI, but they self-skip if the MF platform is unavailable on the host.
/// </summary>
[TestFixture]
public class MfByteStreamHintTests
{
    private bool mediaFoundationAvailable;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        try
        {
            MediaFoundationApi.Startup();
            mediaFoundationAvailable = true;
        }
        catch (Exception)
        {
            // Media Foundation platform not present (e.g. a Server SKU without the
            // Media Foundation feature). Skip rather than fail the run.
            mediaFoundationAvailable = false;
        }
    }

    [SetUp]
    public void SetUp()
    {
        if (!mediaFoundationAvailable)
        {
            Assert.Ignore("Media Foundation platform is not available on this host");
        }
    }

    // A buffer whose leading bytes are the Ogg page capture pattern "OggS", so the
    // content sniffer identifies it as audio/ogg unless a hint suppresses sniffing.
    private static MemoryStream OggStream()
    {
        var bytes = new byte[64];
        bytes[0] = 0x4F; // 'O'
        bytes[1] = 0x67; // 'g'
        bytes[2] = 0x67; // 'g'
        bytes[3] = 0x53; // 'S'
        return new MemoryStream(bytes);
    }

    private static string ReadStringAttribute(MfByteStreamFromStream wrapper, Guid key)
    {
        int hr = wrapper.GetAllocatedString(key, out nint value, out _);
        if (hr < 0)
        {
            return null; // MF_E_ATTRIBUTENOTFOUND - attribute was not set
        }
        try
        {
            return Marshal.PtrToStringUni(value);
        }
        finally
        {
            Marshal.FreeCoTaskMem(value);
        }
    }

    [Test]
    public void NoHint_SniffsOggAndSetsContentType()
    {
        using var wrapper = new MfByteStreamFromStream(OggStream());
        Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.ContentType),
            Is.EqualTo("audio/ogg"));
    }

    [Test]
    public void ContentTypeHint_OverridesSniffing()
    {
        // The stream sniffs as audio/ogg, but the explicit MIME hint must win.
        using var wrapper = new MfByteStreamFromStream(OggStream(), contentType: "audio/mpeg");
        Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.ContentType),
            Is.EqualTo("audio/mpeg"));
    }

    [Test]
    public void OriginNameHint_IsSetAndSuppressesSniffing()
    {
        using var wrapper = new MfByteStreamFromStream(OggStream(), originName: "track.ogg");
        Assert.Multiple(() =>
        {
            Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.OriginName),
                Is.EqualTo("track.ogg"));
            // Supplying an origin-name hint skips the sniffer, so no content type is set.
            Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.ContentType),
                Is.Null);
        });
    }

    [Test]
    public void BothHints_AreBothApplied()
    {
        using var wrapper = new MfByteStreamFromStream(OggStream(),
            contentType: "audio/ogg", originName: "track.ogg");
        Assert.Multiple(() =>
        {
            Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.ContentType),
                Is.EqualTo("audio/ogg"));
            Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.OriginName),
                Is.EqualTo("track.ogg"));
        });
    }

    [Test]
    public void NoHint_UnrecognisedStream_SetsNoContentType()
    {
        // Bytes that match no known container: the sniffer returns nothing and no hint is set.
        var noise = new byte[64];
        for (int i = 0; i < noise.Length; i++)
        {
            noise[i] = (byte)((i * 7 + 3) & 0x7F);
        }
        using var wrapper = new MfByteStreamFromStream(new MemoryStream(noise));
        Assert.That(ReadStringAttribute(wrapper, MfByteStreamAttributes.ContentType), Is.Null);
    }
}
