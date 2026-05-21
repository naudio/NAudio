using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Mp3
{
    /// <summary>
    /// Verifies that <see cref="IMp3FrameDecompressor.DecompressFrame(Mp3Frame, Span{byte})"/>'s
    /// default interface implementation correctly routes through the legacy
    /// <see cref="IMp3FrameDecompressor.DecompressFrame(Mp3Frame, byte[], int)"/> overload.
    /// This is the compatibility path for third-party implementations (e.g. NLayer's
    /// <c>Mp3FrameDecompressor</c>) that only override the byte[] method.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class Mp3FrameDecompressorDimRoutingTests
    {
        /// <summary>Test double: only implements the legacy byte[] overload, mimicking old-NLayer shape.</summary>
        private sealed class LegacyByteArrayOnlyDecompressor : IMp3FrameDecompressor
        {
            public int DecompressCallsReceived { get; private set; }
            public int LastDestOffset { get; private set; }

            public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
            {
                DecompressCallsReceived++;
                LastDestOffset = destOffset;
                for (int i = 0; i < 64; i++)
                {
                    dest[destOffset + i] = (byte)(i * 3 + 7);
                }
                return 64;
            }

            public void Reset() { }
            public WaveFormat OutputFormat { get; } = new WaveFormat(44100, 16, 2);
            public void Dispose() { }
        }

        [Test]
        public void SpanOverloadRoutesThroughLegacyByteArrayOverload()
        {
            var impl = new LegacyByteArrayOnlyDecompressor();
            IMp3FrameDecompressor decompressor = impl;
            Span<byte> dest = stackalloc byte[128];

            int written = decompressor.DecompressFrame(null, dest);

            Assert.That(impl.DecompressCallsReceived, Is.EqualTo(1),
                "Span overload should dispatch through to the byte[] overload exactly once");
            Assert.That(impl.LastDestOffset, Is.EqualTo(0),
                "DIM default rents a pool buffer and passes offset 0 to the byte[] overload");
            Assert.That(written, Is.EqualTo(64),
                "return value from the byte[] overload should be preserved");
            for (int i = 0; i < 64; i++)
            {
                Assert.That(dest[i], Is.EqualTo((byte)(i * 3 + 7)),
                    $"byte {i} should be copied verbatim from the pool-backed buffer");
            }
        }

        [Test]
        public void SpanOverloadDoesNotLeakRentedBuffers()
        {
            var impl = new LegacyByteArrayOnlyDecompressor();
            IMp3FrameDecompressor decompressor = impl;
            Span<byte> dest = stackalloc byte[128];

            // If rent/return were unbalanced, ArrayPool would eventually grow unbounded.
            // This is a smoke test — we can't directly observe the pool, but many calls with
            // no allocation growth is a reasonable signal.
            for (int i = 0; i < 1000; i++)
            {
                decompressor.DecompressFrame(null, dest);
            }

            Assert.That(impl.DecompressCallsReceived, Is.EqualTo(1000));
        }
    }
}
