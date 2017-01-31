using System;
using System.Linq;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    public static class SampleProviderTestHelpers
    {
        public static void AssertReadsExpected(this ISampleProvider sampleProvider, float[] expected)
        {
            AssertReadsExpected(sampleProvider, expected, expected.Length);
        }

        public static void AssertReadsExpected(this ISampleProvider sampleProvider, float[] expected, int readSize)
        {
            var buffer = new float[readSize];
            var read = sampleProvider.Read(buffer, 0, readSize);
            Assert.AreEqual(expected.Length, read, "Number of samples read");
            for (int n = 0; n < read; n++)
            {
                Assert.AreEqual(expected[n], buffer[n], $"Buffer at index {n}");
            }
        }
    }
}
