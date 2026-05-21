using System;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    /// <summary>
    /// Tests for <see cref="AsioAudioCapturedEventArgs"/> — most importantly the use-after-callback guard
    /// (<c>ThrowIfInvalid</c>), which is the entire safety story for the <c>Span&lt;float&gt;</c> API.
    /// Constructs the event args directly via the internal seam since the live ASIO callback path needs hardware.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class AsioAudioCapturedEventArgsTests
    {
        [Test]
        public void Members_AreAccessibleWhileContextIsValid()
        {
            using var ctx = MakeValidContext(channels: 2, frames: 64);
            var args = new AsioAudioCapturedEventArgs(ctx.Context);

            Assert.That(args.Frames, Is.EqualTo(64));
            Assert.That(args.SampleRate, Is.EqualTo(48000));
            Assert.That(args.ChannelCount, Is.EqualTo(2));
            Assert.That(args.SamplePosition, Is.EqualTo(123456L));
        }

        [Test]
        public void GetChannel_ReturnsSpanOfCorrectLength()
        {
            using var ctx = MakeValidContext(channels: 3, frames: 32);
            var args = new AsioAudioCapturedEventArgs(ctx.Context);

            var span = args.GetChannel(1);
            Assert.That(span.Length, Is.EqualTo(32));
        }

        [Test]
        public void GetChannel_ReturnsBufferContents()
        {
            using var ctx = MakeValidContext(channels: 2, frames: 4);
            // Fill channel 1 with a known pattern so the test can assert on it.
            ctx.Context.InputFloatBuffers[1][0] = 0.1f;
            ctx.Context.InputFloatBuffers[1][1] = 0.2f;
            ctx.Context.InputFloatBuffers[1][2] = 0.3f;
            ctx.Context.InputFloatBuffers[1][3] = 0.4f;

            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            var span = args.GetChannel(1);

            Assert.That(span.ToArray(), Is.EqualTo(new[] { 0.1f, 0.2f, 0.3f, 0.4f }));
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public void GetChannel_ThrowsForOutOfRangeIndex(int badIndex)
        {
            using var ctx = MakeValidContext(channels: 2, frames: 32);
            var args = new AsioAudioCapturedEventArgs(ctx.Context);

            Assert.Throws<ArgumentOutOfRangeException>(() => args.GetChannel(badIndex));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void RawInput_ThrowsForOutOfRangeIndex(int badIndex)
        {
            using var ctx = MakeValidContext(channels: 2, frames: 32);
            var args = new AsioAudioCapturedEventArgs(ctx.Context);

            Assert.Throws<ArgumentOutOfRangeException>(() => args.RawInput(badIndex));
        }

        // -- ThrowIfInvalid: every member must reject access after the callback returns ------------------------

        [Test]
        public void Frames_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => { _ = args.Frames; });
        }

        [Test]
        public void SampleRate_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => { _ = args.SampleRate; });
        }

        [Test]
        public void ChannelCount_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => { _ = args.ChannelCount; });
        }

        [Test]
        public void SamplePosition_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => { _ = args.SamplePosition; });
        }

        [Test]
        public void GetChannel_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => args.GetChannel(0));
        }

        [Test]
        public void RawInput_ThrowsAfterContextInvalidated()
        {
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            Assert.Throws<InvalidOperationException>(() => args.RawInput(0));
        }

        [Test]
        public void ThrowIfInvalid_ExceptionMentionsHandlerLifetime()
        {
            // The whole point of this guard is to nudge users toward the right mental model — verify the message
            // explains the rule rather than just being a generic InvalidOperationException.
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);
            ctx.Context.Valid = false;

            var ex = Assert.Throws<InvalidOperationException>(() => { _ = args.Frames; });
            Assert.That(ex!.Message, Does.Contain("handler"));
        }

        [Test]
        public void Members_StillThrowAfterToggleBackOnAndOff()
        {
            // The Valid flag is read on every member access, not cached — a transition Valid=true→false→true→false
            // must still leave members throwing on the final false read.
            using var ctx = MakeValidContext();
            var args = new AsioAudioCapturedEventArgs(ctx.Context);

            ctx.Context.Valid = false;
            Assert.Throws<InvalidOperationException>(() => { _ = args.Frames; });

            ctx.Context.Valid = true;
            Assert.That(args.Frames, Is.EqualTo(ctx.Context.Frames));

            ctx.Context.Valid = false;
            Assert.Throws<InvalidOperationException>(() => { _ = args.Frames; });
        }

        // -- Test infrastructure ---------------------------------------------------------------------------------

        /// <summary>
        /// Builds an <see cref="AsioCallbackContext"/> with allocated input buffers (managed float[] for the float
        /// path, pinned native bytes for the raw path) and Valid=true. Disposing frees the pinned native memory.
        /// </summary>
        private static TestContext MakeValidContext(int channels = 2, int frames = 32)
        {
            const AsioSampleType inputFormat = AsioSampleType.Float32LSB;
            int bytesPerChannel = frames * AsioNativeToFloatConverter.BytesPerSample(inputFormat);

            var nativePtrs = new IntPtr[channels];
            var nativeBlocks = new IntPtr[channels];
            for (int ch = 0; ch < channels; ch++)
            {
                nativeBlocks[ch] = Marshal.AllocHGlobal(bytesPerChannel);
                nativePtrs[ch] = nativeBlocks[ch];
            }

            var floatBuffers = new float[channels][];
            for (int ch = 0; ch < channels; ch++)
                floatBuffers[ch] = new float[frames];

            var ctx = new AsioCallbackContext
            {
                Frames = frames,
                SampleRate = 48000,
                InputChannelCount = channels,
                OutputChannelCount = 0,
                SamplePosition = 123456L,
                InputFormat = inputFormat,
                InputFloatBuffers = floatBuffers,
                InputNativeBuffers = nativePtrs,
                InputNativeBytesPerChannel = bytesPerChannel,
                Valid = true,
            };
            return new TestContext(ctx, nativeBlocks);
        }

        private sealed class TestContext : IDisposable
        {
            public AsioCallbackContext Context { get; }
            private readonly IntPtr[] nativeBlocks;

            public TestContext(AsioCallbackContext context, IntPtr[] nativeBlocks)
            {
                Context = context;
                this.nativeBlocks = nativeBlocks;
            }

            public void Dispose()
            {
                foreach (var p in nativeBlocks)
                {
                    if (p != IntPtr.Zero) Marshal.FreeHGlobal(p);
                }
            }
        }
    }
}
