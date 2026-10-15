using System;
using NAudio.Utils;
using NUnit.Framework;

namespace NAudio.Core.Tests.Utils;

[TestFixture]
public class ByteEncodingTests
{
    [Test]
    public void CanDecodeString()
    {
        var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', };
        Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("Hello"));
    }

    [Test]
    public void CanTruncate()
    {
        var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
        Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("Hello"));
    }

    [Test]
    public void CanTruncateWithThreeParamOverride()
    {
        var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
        Assert.That(ByteEncoding.Instance.GetString(b, 0, b.Length), Is.EqualTo("Hello"));
    }

    [Test]
    public void CanDecodeWithNonZeroIndex()
    {
        var b = new byte[] { (byte)'x', (byte)'H', (byte)'i', (byte)'y' };
        Assert.That(ByteEncoding.Instance.GetString(b, 1, 2), Is.EqualTo("Hi"));
    }

    [Test]
    public void TruncatesAtNullInMiddleOfRange()
    {
        var b = new byte[] { (byte)'x', (byte)'H', (byte)'i', 0, (byte)'y' };
        Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("xHi"));
        Assert.That(ByteEncoding.Instance.GetString(b, 1, 4), Is.EqualTo("Hi"));
    }

    [Test]
    public void EmptyArrayGivesEmptyString()
    {
        Assert.That(ByteEncoding.Instance.GetString(Array.Empty<byte>()), Is.EqualTo(string.Empty));
        Assert.That(ByteEncoding.Instance.GetString(Array.Empty<byte>(), 0, 0), Is.EqualTo(string.Empty));
    }

    [Test]
    public void HighBytesMapToLatin1Characters()
    {
        var b = new byte[] { 0xA9, 0xFF };
        Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("\u00A9\u00FF"));
    }

    [Test]
    public void NullArrayThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ByteEncoding.Instance.GetString(null!));
        Assert.Throws<ArgumentNullException>(() => ByteEncoding.Instance.GetString(null!, 0, 0));
    }
}
