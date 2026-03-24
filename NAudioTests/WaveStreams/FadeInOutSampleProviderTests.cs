using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
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
}
