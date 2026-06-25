using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests;

[TestFixture]
public class AlsaInteropTests : AlsaTestBase
{
    [Test]
    public void StrErrorRoundTripsThroughResolver()
    {
        // Exercises the libasound.so.2 NativeLibrary resolver.
        Assert.That(AlsaInterop.ErrorString(0), Is.Not.Empty);
        Assert.That(AlsaInterop.ErrorString(-22), Is.Not.Empty);
    }

    [Test]
    public void ThrowIfErrorThrowsTypedOnNegative()
    {
        var ex = Assert.Throws<AlsaException>(() => AlsaException.ThrowIfError(-22, "snd_pcm_open"));
        Assert.That(ex.ErrorCode, Is.EqualTo(-22));
        Assert.That(ex.Function, Is.EqualTo("snd_pcm_open"));
        Assert.That(ex.Message, Does.Contain("snd_pcm_open"));
    }

    [Test]
    public void ThrowIfErrorIgnoresSuccess()
        => Assert.DoesNotThrow(() => AlsaException.ThrowIfError(0, "ok"));
}
