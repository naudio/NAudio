using System;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    /// <summary>
    /// Tests for the pure validation helpers on <see cref="AsioDevice"/>: <c>ValidateChannelIndices</c> and
    /// <c>ValidateUniformChannelFormat</c>. Exercised directly via <c>internal</c> access — the call paths through
    /// <c>InitRecording</c> / <c>InitDuplex</c> require real ASIO hardware.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class AsioDeviceValidationTests
    {
        // -- ValidateChannelIndices ------------------------------------------------------------------------------

        [Test]
        public void ValidateChannelIndices_AcceptsValidContiguousRange()
        {
            Assert.DoesNotThrow(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, 1, 2, 3 }, maxChannels: 8, "input", isInput: true));
        }

        [Test]
        public void ValidateChannelIndices_AcceptsValidNonContiguousRange()
        {
            Assert.DoesNotThrow(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, 3, 5, 7 }, maxChannels: 8, "output", isInput: false));
        }

        [Test]
        public void ValidateChannelIndices_AcceptsBoundaryIndex()
        {
            // maxChannels - 1 must be valid; maxChannels itself must not be.
            Assert.DoesNotThrow(() =>
                AsioDevice.ValidateChannelIndices(new[] { 7 }, maxChannels: 8, "param", isInput: false));
        }

        [Test]
        public void ValidateChannelIndices_RejectsEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                AsioDevice.ValidateChannelIndices(Array.Empty<int>(), maxChannels: 4, "myParam", isInput: true));
            Assert.That(ex!.Message, Does.Contain("myParam"));
            Assert.That(ex.Message, Does.Contain("at least one"));
        }

        [Test]
        public void ValidateChannelIndices_RejectsNegativeIndex()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, -1, 2 }, maxChannels: 4, "myParam", isInput: true));
            Assert.That(ex!.ParamName, Is.EqualTo("myParam"));
            Assert.That(ex.Message, Does.Contain("Input"));
            Assert.That(ex.Message, Does.Contain("position 1"));
        }

        [Test]
        public void ValidateChannelIndices_RejectsIndexEqualToMax()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, 4 }, maxChannels: 4, "myParam", isInput: false));
            Assert.That(ex!.Message, Does.Contain("Output"));
            Assert.That(ex.Message, Does.Contain("[0, 3]"));
        }

        [Test]
        public void ValidateChannelIndices_RejectsIndexAboveMax()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, 1, 9 }, maxChannels: 4, "myParam", isInput: false));
        }

        [Test]
        public void ValidateChannelIndices_RejectsDuplicateIndices()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                AsioDevice.ValidateChannelIndices(new[] { 0, 2, 2, 3 }, maxChannels: 4, "myParam", isInput: true));
            Assert.That(ex!.ParamName, Is.EqualTo("myParam"));
            Assert.That(ex.Message, Does.Contain("duplicate"));
            Assert.That(ex.Message, Does.Contain("2"));
        }

        [Test]
        public void ValidateChannelIndices_RejectsDuplicateAtNonAdjacentPositions()
        {
            // Catches the inner-loop logic that compares against every prior index, not just the previous one.
            Assert.Throws<ArgumentException>(() =>
                AsioDevice.ValidateChannelIndices(new[] { 5, 0, 2, 5 }, maxChannels: 8, "myParam", isInput: false));
        }

        // -- ValidateUniformChannelFormat ------------------------------------------------------------------------

        [Test]
        public void ValidateUniformChannelFormat_ReturnsFormatWhenAllSelectedChannelsMatch()
        {
            var infos = MakeInfos(AsioSampleType.Int32LSB, AsioSampleType.Int32LSB, AsioSampleType.Int32LSB);
            var format = AsioDevice.ValidateUniformChannelFormat(new[] { 0, 1, 2 }, infos, isInput: true);
            Assert.That(format, Is.EqualTo(AsioSampleType.Int32LSB));
        }

        [Test]
        public void ValidateUniformChannelFormat_IgnoresUnselectedChannelsThatDiffer()
        {
            // Channel 2 has a different format, but it's not selected, so this must succeed.
            var infos = MakeInfos(AsioSampleType.Float32LSB, AsioSampleType.Float32LSB, AsioSampleType.Int16LSB);
            var format = AsioDevice.ValidateUniformChannelFormat(new[] { 0, 1 }, infos, isInput: false);
            Assert.That(format, Is.EqualTo(AsioSampleType.Float32LSB));
        }

        [Test]
        public void ValidateUniformChannelFormat_AcceptsSingleChannel()
        {
            var infos = MakeInfos(AsioSampleType.Int24LSB);
            var format = AsioDevice.ValidateUniformChannelFormat(new[] { 0 }, infos, isInput: true);
            Assert.That(format, Is.EqualTo(AsioSampleType.Int24LSB));
        }

        [Test]
        public void ValidateUniformChannelFormat_RejectsMixedFormatsWithInputWording()
        {
            var infos = MakeInfos(AsioSampleType.Int32LSB, AsioSampleType.Float32LSB);
            var ex = Assert.Throws<NotSupportedException>(() =>
                AsioDevice.ValidateUniformChannelFormat(new[] { 0, 1 }, infos, isInput: true));
            Assert.That(ex!.Message, Does.Contain("input"));
            Assert.That(ex.Message, Does.Contain("Int32LSB"));
            Assert.That(ex.Message, Does.Contain("Float32LSB"));
        }

        [Test]
        public void ValidateUniformChannelFormat_RejectsMixedFormatsWithOutputWording()
        {
            var infos = MakeInfos(AsioSampleType.Int32LSB, AsioSampleType.Float32LSB);
            var ex = Assert.Throws<NotSupportedException>(() =>
                AsioDevice.ValidateUniformChannelFormat(new[] { 0, 1 }, infos, isInput: false));
            Assert.That(ex!.Message, Does.Contain("output"));
        }

        [Test]
        public void ValidateUniformChannelFormat_DetectsMismatchAtNonAdjacentChannelPositions()
        {
            // Catches a regression where the comparison stops early (e.g., compares only neighbours).
            var infos = MakeInfos(
                AsioSampleType.Float32LSB,  // 0
                AsioSampleType.Float32LSB,  // 1
                AsioSampleType.Float32LSB,  // 2
                AsioSampleType.Int16LSB);   // 3 — only this one differs
            Assert.Throws<NotSupportedException>(() =>
                AsioDevice.ValidateUniformChannelFormat(new[] { 0, 1, 2, 3 }, infos, isInput: true));
        }

        [Test]
        public void ValidateUniformChannelFormat_HandlesNonContiguousSelection()
        {
            // Selecting non-contiguous indices still picks up format mismatches.
            var infos = MakeInfos(
                AsioSampleType.Int32LSB,    // 0 — selected
                AsioSampleType.Float32LSB,  // 1 — not selected
                AsioSampleType.Int16LSB);   // 2 — selected, differs from 0
            Assert.Throws<NotSupportedException>(() =>
                AsioDevice.ValidateUniformChannelFormat(new[] { 0, 2 }, infos, isInput: false));
        }

        private static AsioChannelInfo[] MakeInfos(params AsioSampleType[] types)
        {
            var result = new AsioChannelInfo[types.Length];
            for (int i = 0; i < types.Length; i++)
                result[i].type = types[i];
            return result;
        }
    }
}
