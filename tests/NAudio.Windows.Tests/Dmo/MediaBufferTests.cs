using System;
using NAudio.Dmo;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Dmo;

[TestFixture]
[Category("UnitTest")]
public class MediaBufferTests
{
    [Test]
    public void LengthCannotExceedMaxLength()
    {
        using var buffer = new MediaBuffer(16);
        buffer.Length = 16;
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Length = 17);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Length = -1);
        Assert.That(buffer.Length, Is.EqualTo(16));
    }

    [Test]
    public void SetLengthRejectsOutOfRangeValues()
    {
        using var buffer = new MediaBuffer(16);
        Assert.That(buffer.SetLength(17), Is.Not.EqualTo(0));
        Assert.That(buffer.SetLength(-1), Is.Not.EqualTo(0));
        Assert.That(buffer.SetLength(16), Is.EqualTo(0));
    }

    [Test]
    public void LoadingTooMuchDataLeavesLengthUnchanged()
    {
        using var buffer = new MediaBuffer(16);
        buffer.LoadData(new byte[8]);
        Assert.Throws<ArgumentException>(() => buffer.LoadData(new byte[32]));
        Assert.That(buffer.Length, Is.EqualTo(8));
    }

    [Test]
    public void UsingADisposedBufferThrows()
    {
        var buffer = new MediaBuffer(16);
        buffer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => buffer.LoadData(new byte[4]));
        Assert.Throws<ObjectDisposedException>(() => buffer.RetrieveData(new byte[4]));
    }
}
