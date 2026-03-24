using System;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    public static class SampleProviderTestHelpers
    {
        public static void AssertReadsExpected(this ISampleSource sampleSource, float[] expected)
        {
            AssertReadsExpected(sampleSource, expected, expected.Length);
        }

        public static void AssertReadsExpected(this ISampleSource sampleSource, float[] expected, int readSize)
        {
            var buffer = new float[readSize];
            var read = sampleSource.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expected.Length), "Number of samples read");
            for (int n = 0; n < read; n++)
            {
                Assert.That(buffer[n], Is.EqualTo(expected[n]), $"Buffer at index {n}");
            }
        }
    }
}
