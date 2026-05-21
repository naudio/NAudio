using System;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// Exercises the two ctors of <see cref="WaveInEventArgs"/> and verifies the accessor
    /// semantics — <see cref="WaveInEventArgs.Buffer"/>, <see cref="WaveInEventArgs.BufferSpan"/>,
    /// <see cref="WaveInEventArgs.BufferMemory"/>, and <see cref="WaveInEventArgs.BytesRecorded"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class WaveInEventArgsTests
    {
        [Test]
        public void ByteArrayCtor_BufferReturnsSameReference()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var args = new WaveInEventArgs(buffer, 5);

            Assert.That(args.Buffer, Is.SameAs(buffer),
                "byte[] ctor must hand back the same array instance — callers rely on this");
            Assert.That(args.BytesRecorded, Is.EqualTo(5));
        }

        [Test]
        public void ByteArrayCtor_BufferSpanSlicedToBytesRecorded()
        {
            var buffer = new byte[] { 10, 20, 30, 40, 50, 60 };
            var args = new WaveInEventArgs(buffer, 4);

            var span = args.BufferSpan;
            Assert.That(span.Length, Is.EqualTo(4));
            Assert.That(span[0], Is.EqualTo((byte)10));
            Assert.That(span[3], Is.EqualTo((byte)40));
        }

        [Test]
        public void ByteArrayCtor_NullBufferThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new WaveInEventArgs(null, 0));
        }

        [Test]
        public void MemoryCtor_BytesRecordedMatchesMemoryLength()
        {
            ReadOnlyMemory<byte> mem = new byte[] { 1, 2, 3, 4, 5 };
            var args = new WaveInEventArgs(mem);

            Assert.That(args.BytesRecorded, Is.EqualTo(5));
        }

        [Test]
        public void MemoryCtor_BufferSpanReflectsInput()
        {
            ReadOnlyMemory<byte> mem = new byte[] { 11, 22, 33 };
            var args = new WaveInEventArgs(mem);

            Assert.That(args.BufferSpan.ToArray(), Is.EqualTo(new byte[] { 11, 22, 33 }));
        }

        [Test]
        public void MemoryCtor_ArrayBackedFullSpan_BufferReturnsUnderlyingArray()
        {
            // When the Memory exactly covers a whole array at offset 0, Buffer should hand back
            // that array (no copy). This preserves the legacy "Buffer is the array you passed in"
            // behaviour when a caller conveniently wraps a byte[] in Memory.
            var underlying = new byte[] { 7, 8, 9 };
            var args = new WaveInEventArgs(underlying.AsMemory());

            Assert.That(args.Buffer, Is.SameAs(underlying),
                "a fully-covering array-backed Memory should return the same array");
        }

        [Test]
        public void MemoryCtor_SlicedArrayBacked_BufferAllocatesCopy()
        {
            // When the Memory is a slice, returning the underlying array would leak bytes outside
            // the recorded range; we materialise a fresh byte[] whose length equals BytesRecorded.
            var underlying = new byte[] { 1, 2, 3, 4, 5, 6 };
            var sliced = underlying.AsMemory(1, 3); // {2, 3, 4}
            var args = new WaveInEventArgs(sliced);

            var buffer = args.Buffer;
            Assert.That(buffer, Is.Not.SameAs(underlying),
                "sliced Memory must not expose the full underlying array via Buffer");
            Assert.That(buffer.Length, Is.EqualTo(3));
            Assert.That(buffer, Is.EqualTo(new byte[] { 2, 3, 4 }));
        }

        [Test]
        public void MemoryCtor_BufferIsCachedAcrossCalls()
        {
            // Materialising on every Buffer access would defeat the purpose; once we've paid the
            // ToArray cost we hang onto the result.
            var underlying = new byte[] { 1, 2, 3, 4, 5 };
            var sliced = underlying.AsMemory(1, 3);
            var args = new WaveInEventArgs(sliced);

            var first = args.Buffer;
            var second = args.Buffer;
            Assert.That(second, Is.SameAs(first),
                "Buffer materialisation should be cached on first access");
        }

        [Test]
        public void MemoryCtor_NonArrayBacked_BufferMaterialisesViaToArray()
        {
            // A Memory backed by something other than a byte[] (here, our own MemoryManager over a
            // pinned buffer) has no underlying array to hand back, so Buffer must allocate.
            var src = new byte[] { 42, 43, 44, 45 };
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            try
            {
                using var manager = new NativeMemoryTestManager(handle.AddrOfPinnedObject(), src.Length);
                var args = new WaveInEventArgs(manager.Memory);

                Assert.That(args.BytesRecorded, Is.EqualTo(4));
                Assert.That(args.BufferSpan.ToArray(), Is.EqualTo(src));
                var buffer = args.Buffer;
                Assert.That(buffer, Is.Not.SameAs(src));
                Assert.That(buffer, Is.EqualTo(src));
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Test-local MemoryManager over a raw pointer. Mirrors the shape of the Wasapi native
        /// manager without depending on NAudio.Wasapi internals.
        /// </summary>
        private sealed unsafe class NativeMemoryTestManager : System.Buffers.MemoryManager<byte>
        {
            private readonly byte* pointer;
            private readonly int length;

            public NativeMemoryTestManager(IntPtr p, int length)
            {
                pointer = (byte*)p;
                this.length = length;
            }

            public override Span<byte> GetSpan() => new Span<byte>(pointer, length);

            public override System.Buffers.MemoryHandle Pin(int elementIndex = 0)
                => new System.Buffers.MemoryHandle(pointer + elementIndex);

            public override void Unpin() { }
            protected override void Dispose(bool disposing) { }
        }
    }
}
