using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using NAudio.Wave.Asio;

namespace NAudio.Benchmarks;

/// <summary>
/// Per-callback allocation gate for the duplex audio path (Phase 0 finding F7 / Phase C exit criterion).
///
/// The hot loop reproduces what <c>AsioDevice.OnBufferUpdateDuplex</c> does each buffer switch:
/// for every selected channel, run the native→float input converter, zero the float staging buffer,
/// run a tiny user processor (gain+copy), then run the float→native output converter. The buffers and
/// converters are pre-allocated in <see cref="GlobalSetup"/> so anything BenchmarkDotNet's MemoryDiagnoser
/// reports as allocated bytes per op is a real regression — the pipeline must read 0 B/op once warm.
///
/// 16 channels × 1024 frames is the documented gate scenario; 2 × 256 covers the common stereo case.
/// </summary>
[MemoryDiagnoser]
public class AsioCallbackBenchmarks
{
    [Params(2, 16)] public int Channels;
    [Params(256, 1024)] public int Frames;
    [Params(AsioSampleType.Int32LSB, AsioSampleType.Float32LSB)] public AsioSampleType Format;

    private float[][] inputFloat = null!;
    private float[][] outputFloat = null!;
    private byte[][] inputNativePinned = null!;
    private byte[][] outputNativePinned = null!;
    private GCHandle[] inputHandles = null!;
    private GCHandle[] outputHandles = null!;
    private nint[] inputPointers = null!;
    private nint[] outputPointers = null!;
    private AsioNativeToFloatConverter.ConverterFn inputConverter = null!;
    private AsioFloatToNativeConverter.ConverterFn outputConverter = null!;
    private const float Gain = 0.5f;

    [GlobalSetup]
    public void Setup()
    {
        inputConverter = AsioNativeToFloatConverter.Select(Format);
        outputConverter = AsioFloatToNativeConverter.Select(Format);

        int bytesPerSample = AsioNativeToFloatConverter.BytesPerSample(Format);
        int bytesPerChannel = Frames * bytesPerSample;

        inputFloat = new float[Channels][];
        outputFloat = new float[Channels][];
        inputNativePinned = new byte[Channels][];
        outputNativePinned = new byte[Channels][];
        inputHandles = new GCHandle[Channels];
        outputHandles = new GCHandle[Channels];
        inputPointers = new nint[Channels];
        outputPointers = new nint[Channels];

        var rng = new Random(1337);
        for (int c = 0; c < Channels; c++)
        {
            inputFloat[c] = new float[Frames];
            outputFloat[c] = new float[Frames];
            inputNativePinned[c] = new byte[bytesPerChannel];
            outputNativePinned[c] = new byte[bytesPerChannel];

            // Pin once for the lifetime of the benchmark — emulates ASIO driver-owned pointers.
            inputHandles[c] = GCHandle.Alloc(inputNativePinned[c], GCHandleType.Pinned);
            outputHandles[c] = GCHandle.Alloc(outputNativePinned[c], GCHandleType.Pinned);
            inputPointers[c] = inputHandles[c].AddrOfPinnedObject();
            outputPointers[c] = outputHandles[c].AddrOfPinnedObject();

            // Seed the input pinned bytes with random samples in the right format so the converter has real work.
            if (Format == AsioSampleType.Float32LSB)
            {
                var floats = MemoryMarshal.Cast<byte, float>(inputNativePinned[c]);
                for (int i = 0; i < Frames; i++) floats[i] = (float)(rng.NextDouble() * 2 - 1);
            }
            else if (Format == AsioSampleType.Int32LSB)
            {
                var ints = MemoryMarshal.Cast<byte, int>(inputNativePinned[c]);
                for (int i = 0; i < Frames; i++) ints[i] = rng.Next(int.MinValue, int.MaxValue);
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        for (int c = 0; c < Channels; c++)
        {
            if (inputHandles[c].IsAllocated) inputHandles[c].Free();
            if (outputHandles[c].IsAllocated) outputHandles[c].Free();
        }
    }

    /// <summary>
    /// One ASIO duplex buffer-switch worth of work. Should report 0 B/op in MemoryDiagnoser when warm.
    /// </summary>
    [Benchmark]
    public void DuplexCallback()
    {
        for (int c = 0; c < Channels; c++)
        {
            inputConverter(inputPointers[c], inputFloat[c].AsSpan(0, Frames), Frames);
        }

        for (int c = 0; c < Channels; c++)
        {
            Array.Clear(outputFloat[c], 0, Frames);
        }

        // Trivial pass-through user processor: out[i] = in[i] * Gain.
        for (int c = 0; c < Channels; c++)
        {
            var inSpan = inputFloat[c].AsSpan(0, Frames);
            var outSpan = outputFloat[c].AsSpan(0, Frames);
            for (int i = 0; i < Frames; i++) outSpan[i] = inSpan[i] * Gain;
        }

        for (int c = 0; c < Channels; c++)
        {
            outputConverter(outputFloat[c].AsSpan(0, Frames), outputPointers[c], Frames);
        }
    }
}
