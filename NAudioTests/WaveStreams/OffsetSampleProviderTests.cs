using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
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
            
            float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanAddPreDelay()
        {
            var source = new TestSampleProvider(32000, 1);
            source.Position = 10;
            var osp = new OffsetSampleProvider(source);
            osp.DelayBySamples = 5;
            
            float[] expected = new float[] { 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15 };
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanSkipOver()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            osp.SkipOverSamples = 17;

            float[] expected = new float[] { 17,18,19,20,21,22,23,24 };
            osp.AssertReadsExpected(expected);
        }

        [Test]
        public void CanTake()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            osp.TakeSamples = 7;

            float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };
            osp.AssertReadsExpected(expected, 10);
        }

        [Test]
        public void CanAddLeadOut()
        {
            var source = new TestSampleProvider(32000, 1, 10);
            var osp = new OffsetSampleProvider(source);
            osp.LeadOutSamples = 5;

            float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0 };
            osp.AssertReadsExpected(expected);
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
            var source = new TestSampleProvider(32000, 1);
            source.Position = 10;
            var osp = new OffsetSampleProvider(source);
            osp.DelayBySamples = 10;

            float[] expected = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected);
            float[] expected2 = new float[] {0, 0, 0, 0, 0,}; 
            osp.AssertReadsExpected(expected2);
            float[] expected3 = new float[] {10, 11, 12, 13, 14, 15}; 
            osp.AssertReadsExpected(expected3);
        }

        [Test]
        public void MaintainsTakeState()
        {
            var source = new TestSampleProvider(32000, 1);
            var osp = new OffsetSampleProvider(source);
            osp.TakeSamples = 15;

            float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            osp.AssertReadsExpected(expected);
            float[] expected2 = new float[] { 8, 9, 10, 11, 12, 13, 14 };
            osp.AssertReadsExpected(expected2, 20);
        }

        // TODO: Test that Read offset parameter is respected
        // TODO: OffsetSampleProvider needs TimeSpan helper methods
        // TODO: OffsetSampleProvider needs to check blockalign
        // TODO: OffsetSampleProvider needs to disallow changing values after initial Read
    }
}
