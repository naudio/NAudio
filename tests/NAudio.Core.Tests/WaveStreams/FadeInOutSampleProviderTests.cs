using System;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class FadeInOutSampleProviderTests
{
    [Test]
    public void CanFadeIn()
    {
        var source = new TestSampleProvider(10, 1); // 10 samples per second
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        fade.BeginFadeIn(1000);
        float[] buffer = new float[20];
        int read = fade.Read(buffer.AsSpan(0, 20));
        Assert.That(read, Is.EqualTo(20));
        Assert.That(buffer[0], Is.EqualTo(0)); // start of fade-in
        Assert.That(buffer[5], Is.EqualTo(50)); // half-way
        Assert.That(buffer[10], Is.EqualTo(100)); // fully fade in
        Assert.That(buffer[15], Is.EqualTo(100)); // fully fade in
    }

    [Test]
    public void CanFadeOut()
    {
        var source = new TestSampleProvider(10, 1); // 10 samples per second
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        fade.BeginFadeOut(1000);
        float[] buffer = new float[20];
        int read = fade.Read(buffer.AsSpan(0, 20));
        Assert.That(read, Is.EqualTo(20));
        Assert.That(buffer[0], Is.EqualTo(100)); // start of fade-out
        Assert.That(buffer[5], Is.EqualTo(50)); // half-way
        Assert.That(buffer[10], Is.EqualTo(0)); // fully fade out
        Assert.That(buffer[15], Is.EqualTo(0)); // fully fade out
    }

    [Test]
    public void FadeDurationCanBeLongerThanOneRead()
    {
        var source = new TestSampleProvider(10, 1); // 10 samples per second
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        fade.BeginFadeIn(1000);
        float[] buffer = new float[4];
        int read = fade.Read(buffer.AsSpan(0, 4));
        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(0)); // start of fade-in
        Assert.That(buffer[1], Is.EqualTo(10));
        Assert.That(buffer[2], Is.EqualTo(20).Within(0.0001));
        Assert.That(buffer[3], Is.EqualTo(30).Within(0.0001));

        read = fade.Read(buffer.AsSpan(0, 4));
        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(40).Within(0.0001));
        Assert.That(buffer[1], Is.EqualTo(50).Within(0.0001));
        Assert.That(buffer[2], Is.EqualTo(60).Within(0.0001));
        Assert.That(buffer[3], Is.EqualTo(70).Within(0.0001));

        read = fade.Read(buffer.AsSpan(0, 4));
        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer[0], Is.EqualTo(80).Within(0.0001));
        Assert.That(buffer[1], Is.EqualTo(90).Within(0.0001));
        Assert.That(buffer[2], Is.EqualTo(100).Within(0.0001));
        Assert.That(buffer[3], Is.EqualTo(100));
    }

    [Test]
    public void WaveFormatReturnsSourceWaveFormat()
    {
        var source = new TestSampleProvider(10, 1); // 10 samples per second
        var fade = new FadeInOutSampleProvider(source);
        Assert.That(fade.WaveFormat, Is.SameAs(source.WaveFormat));
    }

    [Test]
    public void FadeWorksOverSamplePairs()
    {
        var source = new TestSampleProvider(10, 2); // 10 samples per second
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        fade.BeginFadeIn(1000);
        float[] buffer = new float[20];
        int read = fade.Read(buffer.AsSpan(0, 20));
        Assert.That(read, Is.EqualTo(20));
        Assert.That(buffer[0], Is.EqualTo(0)); // start of fade-in
        Assert.That(buffer[1], Is.EqualTo(0)); // start of fade-in
        Assert.That(buffer[10], Is.EqualTo(50)); // half-way
        Assert.That(buffer[11], Is.EqualTo(50)); // half-way
        Assert.That(buffer[18], Is.EqualTo(90).Within(0.0001)); // fully fade in
        Assert.That(buffer[19], Is.EqualTo(90).Within(0.0001)); // fully fade in
    }

    [Test]
    public void FadeInCompleteFiresOnceWhenFadeInFinishes()
    {
        var source = new TestSampleProvider(10, 1);
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source, initiallySilent: true);
        int fireCount = 0;
        fade.FadeInComplete += (s, e) => fireCount++;
        fade.BeginFadeIn(1000); // 10 samples' worth
        var buffer = new float[20];

        // The fade-in transitions to FullVolume on the read that consumes past its 10th sample.
        fade.Read(buffer.AsSpan(0, 20));
        Assert.That(fireCount, Is.EqualTo(1));

        // Subsequent reads at full volume must not re-fire.
        fade.Read(buffer.AsSpan(0, 20));
        Assert.That(fireCount, Is.EqualTo(1));
    }

    [Test]
    public void FadeOutCompleteFiresOnceWhenFadeOutFinishes()
    {
        var source = new TestSampleProvider(10, 1);
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        int fireCount = 0;
        fade.FadeOutComplete += (s, e) => fireCount++;
        fade.BeginFadeOut(1000);
        var buffer = new float[20];

        fade.Read(buffer.AsSpan(0, 20));
        Assert.That(fireCount, Is.EqualTo(1));

        // Once silent, further reads must not re-fire.
        fade.Read(buffer.AsSpan(0, 20));
        Assert.That(fireCount, Is.EqualTo(1));
    }

    [Test]
    public void FadeCompleteEventsDoNotFireMidFade()
    {
        var source = new TestSampleProvider(10, 1);
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        int inFires = 0, outFires = 0;
        fade.FadeInComplete += (s, e) => inFires++;
        fade.FadeOutComplete += (s, e) => outFires++;
        fade.BeginFadeOut(1000); // 10 samples of fade

        // Read only 5 samples — the fade-out is still in progress.
        fade.Read(new float[5].AsSpan());
        Assert.That(inFires, Is.EqualTo(0));
        Assert.That(outFires, Is.EqualTo(0));

        // Finish the fade in the second read (16 more samples covers the remaining fade tail).
        fade.Read(new float[16].AsSpan());
        Assert.That(outFires, Is.EqualTo(1));
        Assert.That(inFires, Is.EqualTo(0));
    }

    [Test]
    public void InitiallySilentDoesNotFireFadeOutComplete()
    {
        var source = new TestSampleProvider(10, 1);
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source, initiallySilent: true);
        int outFires = 0;
        fade.FadeOutComplete += (s, e) => outFires++;

        fade.Read(new float[20].AsSpan());
        Assert.That(outFires, Is.EqualTo(0));
    }

    [Test]
    public void BufferIsZeroedAfterFadeOut()
    {
        var source = new TestSampleProvider(10, 1); // 10 samples per second
        source.UseConstValue = true;
        source.ConstValue = 100;
        var fade = new FadeInOutSampleProvider(source);
        fade.BeginFadeOut(1000);
        float[] buffer = new float[20];
        int read = fade.Read(buffer.AsSpan(0, 20));
        Assert.That(read, Is.EqualTo(20));
        Assert.That(buffer[0], Is.EqualTo(100)); // start of fade-in
        Assert.That(buffer[5], Is.EqualTo(50)); // half-way
        Assert.That(buffer[10], Is.EqualTo(0)); // half-way
        read = fade.Read(buffer.AsSpan(0, 20));
        Assert.That(read, Is.EqualTo(20));
        Assert.That(buffer[0], Is.EqualTo(0));
    }
}
