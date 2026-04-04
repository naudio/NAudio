using System;
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
            var read = sampleProvider.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expected.Length), "Number of samples read");
            for (int n = 0; n < read; n++)
            {
                Assert.That(buffer[n], Is.EqualTo(expected[n]), $"Buffer at index {n}");
            }
        }
    }
}
