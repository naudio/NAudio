using System;
using System.Linq;
using NAudio.Wave.SampleProviders;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class OffsetSampleProviderTests
    {
        [Test]
        public void DefaultShouldPassStraightThrough()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            
            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanAddPreDelay()
        {
            var source = new TestSampleProvider(32000, 1) {Position = 10};
            var osp = new OffsetSampleProvider(source) {DelayBySamples = 5};

            var expected = new float[] { 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15 };
            osp.AssertReadsExpected(expected);
        }


        [Test]
        public void CanAddPreDelayUsingTimeSpan()
        {
            var source = new TestSampleProvider(100, 1) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(1) };

            var expected = Enumerable.Range(0,100).Select(x => 0f)
                            .Concat(Enumerable.Range(10,10).Select(x => (float)x)).ToArray();
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanAddPreDelayToStereoSourceUsingTimeSpan()
        {
            var source = new TestSampleProvider(100, 2) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(1) };

            var expected = Enumerable.Range(0, 200).Select(x => 0f)
                            .Concat(Enumerable.Range(10, 10).Select(x => (float)x)).ToArray();
            osp.AssertReadsExpected(expected);
        }
        
        [Test]
        public void SettingPreDelayUsingTimeSpanReturnsCorrectTimeSpan()
        {
            var source = new TestSampleProvider(100, 2) { Position = 10 };
            var osp = new OffsetSampleProvider(source) { DelayBy = TimeSpan.FromSeconds(2.5) };

            Assert.AreEqual(2500, (int) osp.DelayBy.TotalMilliseconds);
            Assert.AreEqual(500, osp.DelayBySamples);
        }

        [Test]
        public void CanSkipOver()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {SkipOverSamples = 17};

            var expected = new float[] { 17,18,19,20,21,22,23,24 };
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanTake()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {TakeSamples = 7};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected, 10);
        }


        [Test]
        public void CanTakeThirtySeconds()
        {
            var source = new TestSampleProvider(16000, 1);
            var osp = new OffsetSampleProvider(source) { Take = TimeSpan.FromSeconds(30) };
            var buffer = new float[16000];
            var totalRead = 0;
            while (true)
            {
                var read = osp.Read(buffer, 0, buffer.Length);
                totalRead += read;
                if (read == 0) break;
                Assert.IsTrue(totalRead <= 480000);

            }
            Assert.AreEqual(480000, totalRead);

        }

        [Test]
        public void CanAddLeadOut()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source) {LeadOutSamples = 5};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 };
            osp.AssertReadsExpected(expected, 100);
            var expected2 = new float[] { };
            osp.AssertReadsExpected(expected2, 100);
        }

        [Test]
        public void WaveFormatIsSampeAsSource()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source);
            Assert.AreEqual(source.WaveFormat, osp.WaveFormat);
        }


        [Test]
        public void MaintainsPredelayState()
        {
            var source = new TestSampleProvider(32000, 1) {Position = 10};
            var osp = new OffsetSampleProvider(source) {DelayBySamples = 10};

            var expected = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected);
            var expected2 = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected2);
            var expected3 = new float[] {10, 11, 12, 13, 14, 15}; 
            osp.AssertReadsExpected(expected3);
        }

        [Test]
        public void MaintainsTakeState()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source) {TakeSamples = 15};

            var expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            osp.AssertReadsExpected(expected);
            var expected2 = new float[] { 8, 9, 10, 11, 12, 13, 14 };
            osp.AssertReadsExpected(expected2, 20);
        }

        [Test]
        public void CantSetDelayBySamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.DelayBySamples = 4);
        }

        [Test]
        public void CantSetLeadOutSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.LeadOutSamples = 4);
        }

        [Test]
        public void CantSetSkipOverSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.SkipOverSamples = 4);
        }

        [Test]
        public void CantSetTakeSamplesAfterCallingRead()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            var buffer = new float[10];
            osp.Read(buffer, 0, buffer.Length);

            Assert.Throws<InvalidOperationException>(() => osp.TakeSamples = 4);
        }

        [Test]
        public void HandlesSkipOverEntireSourceCorrectly()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source);
            osp.SkipOverSamples = 20;

            var expected = new float[] { };
            osp.AssertReadsExpected(expected, 20);
        }


        [Test]
        public void CantSetNonBlockAlignedDelayBySamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.DelayBySamples = 3);
            Assert.That(ex.Message.Contains("DelayBySamples"));
        }

        [Test]
        public void CantSetNonBlockAlignedSkipOverSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.SkipOverSamples = 3);
            Assert.That(ex.Message.Contains("SkipOverSamples"));
        }

        [Test]
        public void CantSetNonBlockAlignedTakeSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.TakeSamples = 3);
            Assert.That(ex.Message.Contains("TakeSamples"));
        }


        [Test]
        public void CantSetNonBlockAlignedLeadOutSamples()
        {
            var source = new TestSampleProvider(32000, 2);
            var osp = new OffsetSampleProvider(source);

            var ex = Assert.Throws<ArgumentException>(() => osp.LeadOutSamples = 3);
            Assert.That(ex.Message.Contains("LeadOutSamples"));
        }

        // TODO: Test that Read offset parameter is respected
    }
}
