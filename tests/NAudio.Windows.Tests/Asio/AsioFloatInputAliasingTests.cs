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
    public unsafe void GetInput_NonFloatNativeFormat_ConvertsOncePerCallbackAndRefreshesAfterReset()
    {
        // The key risk of the lazy path: a missed per-callback reset of InputConverted would serve stale audio.
        // This drives GetInput across two simulated buffer-switches (resetting the guard exactly as the real
        // callback does) plus a within-callback re-read, asserting: convert-on-first-access, cache-within-callback
        // (native re-read is NOT observed until reset), and a correct refresh once the guard is reset.
        var converter = AsioNativeToFloatConverter.Select(AsioSampleType.Int32LSB);
        var valuesA = new[] { int.MaxValue, int.MaxValue / 2, -int.MaxValue / 4, 0 };
        var valuesB = new[] { 0, -int.MaxValue / 2, int.MaxValue / 4, int.MaxValue };

        IntPtr native = Marshal.AllocHGlobal(Frames * sizeof(int));
        try
        {
            var ctx = new AsioCallbackContext
            {
                Frames = Frames,
                SampleRate = 48000,
                InputChannelCount = 1,
                OutputChannelCount = 0,
                InputFormat = AsioSampleType.Int32LSB,
                InputConverter = converter,
                InputConverted = new bool[1],
                InputFloatBuffers = new[] { new float[Frames] },
                InputNativeBuffers = new[] { native },
                OutputFloatBuffers = Array.Empty<float[]>(),
                OutputNativeBuffers = Array.Empty<IntPtr>(),
                InputNativeBytesPerChannel = Frames * sizeof(int),
                Valid = true,
            };
            var buffers = new AsioProcessBuffers(ctx);

            var expectedA = new float[Frames];
            var expectedB = new float[Frames];
            converter(AsIntPtr(valuesA, out var pinA), expectedA, Frames);
            converter(AsIntPtr(valuesB, out var pinB), expectedB, Frames);
            pinA.Free();
            pinB.Free();

            // Callback 1: native holds A, guard freshly reset.
            new Span<int>(valuesA).CopyTo(new Span<int>((void*)native, Frames));
            ctx.InputConverted[0] = false;
            var r1 = buffers.GetInput(0).ToArray();
            Assert.That(r1, Is.EqualTo(expectedA), "first access converts from native");
            Assert.That(ctx.InputConverted[0], Is.True, "guard is set after conversion");

            // Still callback 1: native changes but guard is NOT reset — GetInput must serve the cached A, not re-read B.
            new Span<int>(valuesB).CopyTo(new Span<int>((void*)native, Frames));
            var r2 = buffers.GetInput(0).ToArray();
            Assert.That(r2, Is.EqualTo(expectedA), "cached within a callback — no re-conversion until the guard is reset");

            // Callback 2: guard reset (as the real buffer-switch does) — GetInput must now reflect B, never stale A.
            ctx.InputConverted[0] = false;
            var r3 = buffers.GetInput(0).ToArray();
            Assert.That(r3, Is.EqualTo(expectedB), "refreshes from native after the per-callback reset");
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    [Test]
    public unsafe void GetChannel_NonFloatNativeFormat_ConvertsLazilyFromNative()
    {
        // Recording-path counterpart: GetChannel converts native→float on access for non-float formats.
        var converter = AsioNativeToFloatConverter.Select(AsioSampleType.Int16LSB);
        var values = new short[] { short.MaxValue, short.MinValue, 0, short.MaxValue / 3 };

        IntPtr native = Marshal.AllocHGlobal(Frames * sizeof(short));
        try
        {
            new Span<short>(values).CopyTo(new Span<short>((void*)native, Frames));
            var expected = new float[Frames];
            converter(native, expected, Frames);

            var ctx = new AsioCallbackContext
            {
                Frames = Frames,
                SampleRate = 48000,
                InputChannelCount = 1,
                OutputChannelCount = 0,
                InputFormat = AsioSampleType.Int16LSB,
                InputConverter = converter,
                InputConverted = new bool[1],
                InputFloatBuffers = new[] { new float[Frames] },
                InputNativeBuffers = new[] { native },
                OutputFloatBuffers = Array.Empty<float[]>(),
                OutputNativeBuffers = Array.Empty<IntPtr>(),
                InputNativeBytesPerChannel = Frames * sizeof(short),
                Valid = true,
            };
            var args = new AsioAudioCapturedEventArgs(ctx);

            Assert.That(args.GetChannel(0).ToArray(), Is.EqualTo(expected));
            Assert.That(ctx.InputConverted[0], Is.True);
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    private static unsafe IntPtr AsIntPtr(int[] values, out GCHandle handle)
    {
        handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        return handle.AddrOfPinnedObject();
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
