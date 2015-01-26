using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAudio.Wave.SampleProviders;

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
            int read = fade.Read(buffer, 0, 20);
            Assert.AreEqual(20, read);
            Assert.AreEqual(0, buffer[0]); // start of fade-in
            Assert.AreEqual(50, buffer[5]); // half-way
            Assert.AreEqual(100, buffer[10]); // fully fade in
            Assert.AreEqual(100, buffer[15]); // fully fade in
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
            int read = fade.Read(buffer, 0, 20);
            Assert.AreEqual(20, read);
            Assert.AreEqual(100, buffer[0]); // start of fade-out
            Assert.AreEqual(50, buffer[5]); // half-way
            Assert.AreEqual(0, buffer[10]); // fully fade out
            Assert.AreEqual(0, buffer[15]); // fully fade out
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
            int read = fade.Read(buffer, 0, 4);
            Assert.AreEqual(4, read);
            Assert.AreEqual(0, buffer[0]); // start of fade-in
            Assert.AreEqual(10, buffer[1]);
            Assert.AreEqual(20, buffer[2], 0.0001);
            Assert.AreEqual(30, buffer[3], 0.0001);

            read = fade.Read(buffer, 0, 4);
            Assert.AreEqual(4, read);
            Assert.AreEqual(40, buffer[0], 0.0001);
            Assert.AreEqual(50, buffer[1], 0.0001);
            Assert.AreEqual(60, buffer[2], 0.0001);
            Assert.AreEqual(70, buffer[3], 0.0001);

            read = fade.Read(buffer, 0, 4);
            Assert.AreEqual(4, read);
            Assert.AreEqual(80, buffer[0], 0.0001);
            Assert.AreEqual(90, buffer[1], 0.0001);
            Assert.AreEqual(100, buffer[2], 0.0001);
            Assert.AreEqual(100, buffer[3]);
        }

        [Test]
        public void WaveFormatReturnsSourceWaveFormat()
        {
            var source = new TestSampleProvider(10, 1); // 10 samples per second
            var fade = new FadeInOutSampleProvider(source);
            Assert.AreSame(source.WaveFormat, fade.WaveFormat);
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
            int read = fade.Read(buffer, 0, 20);
            Assert.AreEqual(20, read);
            Assert.AreEqual(0, buffer[0]); // start of fade-in
            Assert.AreEqual(0, buffer[1]); // start of fade-in
            Assert.AreEqual(50, buffer[10]); // half-way
            Assert.AreEqual(50, buffer[11]); // half-way
            Assert.AreEqual(90, buffer[18], 0.0001); // fully fade in
            Assert.AreEqual(90, buffer[19], 0.0001); // fully fade in
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
            int read = fade.Read(buffer, 0, 20);
            Assert.AreEqual(20, read);
            Assert.AreEqual(100, buffer[0]); // start of fade-in
            Assert.AreEqual(50, buffer[5]); // half-way
            Assert.AreEqual(0, buffer[10]); // half-way
            read = fade.Read(buffer, 0, 20);
            Assert.AreEqual(20, read);
            Assert.AreEqual(0, buffer[0]);
        }
    }
}
