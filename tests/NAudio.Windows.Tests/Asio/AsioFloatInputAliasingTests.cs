using System;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Asio;

/// <summary>
/// Covers the zero-copy capture fast path: when the driver's native input format is already
/// <see cref="AsioSampleType.Float32LSB"/>, <see cref="AsioProcessBuffers.GetInput"/> and
/// <see cref="AsioAudioCapturedEventArgs.GetChannel"/> alias the driver buffer directly instead
/// of returning a copy staged through <c>InputFloatBuffers</c>. As with the other duplex tests,
/// these build an <c>AsioCallbackContext</c> by hand so no real ASIO hardware is required.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class AsioFloatInputAliasingTests
{
    private const int Frames = 4;

    [Test]
    public unsafe void GetInput_FloatNativeFormat_AliasesDriverBufferWithoutCopying()
    {
        var nativeValues = new[] { 0.1f, -0.2f, 0.3f, -0.4f };
        IntPtr native = Marshal.AllocHGlobal(Frames * sizeof(float));
        try
        {
            new Span<float>(nativeValues).CopyTo(new Span<float>((void*)native, Frames));

            // Sentinel-fill the staging buffer so we can prove the native→float copy is skipped:
            // if GetInput returned the staging buffer, these values would surface instead of the native ones.
            var staging = new float[Frames];
            Array.Fill(staging, 12345f);

            var ctx = FloatInputContext(native, staging);
            var buffers = new AsioProcessBuffers(ctx);

            // Reads the driver's native samples directly.
            Assert.That(buffers.GetInput(0).ToArray(), Is.EqualTo(nativeValues));
            // The library staging buffer was never written — the copy really was skipped.
            Assert.That(staging, Is.All.EqualTo(12345f));

            // It is a live alias, not a start-of-callback snapshot: mutating driver memory is visible through a fresh span.
            new Span<float>((void*)native, Frames)[2] = 0.99f;
            Assert.That(buffers.GetInput(0)[2], Is.EqualTo(0.99f));
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    [Test]
    public unsafe void GetChannel_FloatNativeFormat_AliasesDriverBufferWithoutCopying()
    {
        var nativeValues = new[] { 0.5f, 0.25f, -0.75f, 1.0f };
        IntPtr native = Marshal.AllocHGlobal(Frames * sizeof(float));
        try
        {
            new Span<float>(nativeValues).CopyTo(new Span<float>((void*)native, Frames));

            var staging = new float[Frames];
            Array.Fill(staging, 12345f);

            var ctx = FloatInputContext(native, staging);
            var args = new AsioAudioCapturedEventArgs(ctx);

            Assert.That(args.GetChannel(0).ToArray(), Is.EqualTo(nativeValues));
            Assert.That(staging, Is.All.EqualTo(12345f));
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    [Test]
    public void GetInput_NonFloatNativeFormat_ReturnsConvertedStagingBuffer()
    {
        // For non-float formats the eager callback loop populates the staging buffer; GetInput must return
        // that buffer and must NOT dereference the native pointer (left as IntPtr.Zero here — an alias attempt
        // would access-violate). This guards the fast path from being applied to the wrong formats.
        var staging = new[] { 1f, 2f, 3f, 4f };
        var ctx = new AsioCallbackContext
        {
            Frames = Frames,
            SampleRate = 48000,
            InputChannelCount = 1,
            OutputChannelCount = 0,
            InputFormat = AsioSampleType.Int32LSB,
            InputFloatBuffers = new[] { staging },
            InputNativeBuffers = new IntPtr[1],
            OutputFloatBuffers = Array.Empty<float[]>(),
            OutputNativeBuffers = Array.Empty<IntPtr>(),
            InputNativeBytesPerChannel = Frames * sizeof(int),
            Valid = true,
        };

        var buffers = new AsioProcessBuffers(ctx);
        Assert.That(buffers.GetInput(0).ToArray(), Is.EqualTo(staging));
    }

    // Mirrors the context InitDuplex / InitRecording build for a single Float32LSB input channel.
    private static AsioCallbackContext FloatInputContext(IntPtr native, float[] staging) => new()
    {
        Frames = Frames,
        SampleRate = 48000,
        InputChannelCount = 1,
        OutputChannelCount = 0,
        InputFormat = AsioSampleType.Float32LSB,
        InputFloatBuffers = new[] { staging },
        InputNativeBuffers = new[] { native },
        OutputFloatBuffers = Array.Empty<float[]>(),
        OutputNativeBuffers = Array.Empty<IntPtr>(),
        InputNativeBytesPerChannel = Frames * sizeof(float),
        Valid = true,
    };
}
