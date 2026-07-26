using System;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Asio;

/// <summary>
/// Unit coverage for output-only duplex sessions (empty <see cref="AsioDuplexOptions.InputChannels"/>).
/// The full <see cref="AsioDevice.InitDuplex"/> path needs real ASIO hardware, so these tests exercise the
/// per-callback view (<see cref="AsioProcessBuffers"/>) directly against a hand-built <c>AsioCallbackContext</c>
/// shaped exactly as <c>InitDuplex</c> shapes it when no inputs are selected: zero input channels, populated outputs.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class AsioOutputOnlyDuplexTests
{
    private const int Frames = 4;

    // Mirrors the context InitDuplex builds for OutputChannels = [0, 1] with no inputs.
    private static AsioCallbackContext OutputOnlyContext(int outputChannels = 2) => new()
    {
        Frames = Frames,
        SampleRate = 48000,
        InputChannelCount = 0,
        OutputChannelCount = outputChannels,
        InputFormat = default,               // unset — no input side
        OutputFormat = AsioSampleType.Float32LSB,
        InputFloatBuffers = Array.Empty<float[]>(),
        OutputFloatBuffers = AllocChannels(outputChannels),
        InputNativeBuffers = Array.Empty<IntPtr>(),
        OutputNativeBuffers = new IntPtr[outputChannels],
        InputNativeBytesPerChannel = 0,
        OutputRawAccessed = new bool[outputChannels],
        Valid = true,
    };

    private static float[][] AllocChannels(int count)
    {
        var buffers = new float[count][];
        for (int i = 0; i < count; i++) buffers[i] = new float[Frames];
        return buffers;
    }

    [Test]
    public void OutputOnly_ReportsZeroInputsAndCorrectOutputCount()
    {
        var ctx = OutputOnlyContext(outputChannels: 4);
        var buffers = new AsioProcessBuffers(ctx);
        // Read the ref-struct properties into plain locals — a ref struct can't be captured in the Assert.Multiple lambda.
        int inputCount = buffers.InputChannelCount;
        int outputCount = buffers.OutputChannelCount;
        int frames = buffers.Frames;
        Assert.Multiple(() =>
        {
            Assert.That(inputCount, Is.EqualTo(0));
            Assert.That(outputCount, Is.EqualTo(4));
            Assert.That(frames, Is.EqualTo(Frames));
        });
    }

    [Test]
    public void OutputOnly_GetOutput_ReturnsWritablePerChannelSpans()
    {
        var ctx = OutputOnlyContext();
        var buffers = new AsioProcessBuffers(ctx);

        var ch0 = buffers.GetOutput(0);
        var ch1 = buffers.GetOutput(1);
        Assert.That(ch0.Length, Is.EqualTo(Frames));
        Assert.That(ch1.Length, Is.EqualTo(Frames));

        // Writes land in the library-owned buffers, independently per channel.
        ch0[0] = 0.25f;
        ch1[3] = -0.5f;
        Assert.That(ctx.OutputFloatBuffers[0][0], Is.EqualTo(0.25f));
        Assert.That(ctx.OutputFloatBuffers[1][3], Is.EqualTo(-0.5f));
        Assert.That(ctx.OutputFloatBuffers[1][0], Is.EqualTo(0f), "writing channel 0 must not touch channel 1");
    }

    [Test]
    public void OutputOnly_GetInput_ThrowsWithOutputOnlyExplanation()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var buffers = new AsioProcessBuffers(OutputOnlyContext());
            buffers.GetInput(0);
        });
        Assert.That(ex!.Message, Does.Contain("output-only"));
    }

    [Test]
    public void OutputOnly_RawInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var buffers = new AsioProcessBuffers(OutputOnlyContext());
            buffers.RawInput(0);
        });
    }

    [Test]
    public void OutputOnly_GetOutput_RejectsOutOfRangeChannel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var buffers = new AsioProcessBuffers(OutputOnlyContext(outputChannels: 2));
            buffers.GetOutput(2);
        });
    }
}
