// Ported to C# for NAudio from NeuralAmpModelerCore
// (https://github.com/sdatkinson/NeuralAmpModelerCore), NAM/wavenet/a2_fast.cpp.
// Original licence:
//
//     MIT License
//
//     Copyright (c) 2023 Steven Atkinson
//
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace NAudio.Nam.Internal;

/// <summary>
/// The NAM "A2" WaveNet: one stack of 23 dilated causal convolutions (LeakyReLU,
/// no gating, a 1x1 residual mix per layer) feeding a 16-tap head convolution.
/// A2-Lite has 3 channels and A2-Full has 8; everything else is identical.
/// </summary>
/// <remarks>
/// This is a C# port of the A2 fast path in NeuralAmpModelerCore
/// (NAM/wavenet/a2_fast.cpp, MIT licence, © Steven Atkinson), and reads weights in
/// exactly the same order. Each frame's channel vector is stored padded to
/// <typeparamref name="TLanes"/>.Count floats so one frame is one SIMD register.
/// Convolution histories live in power-of-two ring buffers indexed by a frame
/// counter shared by every layer, so processing never shifts memory.
/// </remarks>
internal sealed class A2WaveNet<TLanes> : NamDsp where TLanes : struct, ISimdLanes<TLanes>
{
    private readonly int channels;
    private readonly Layer[] layers;
    private readonly float[] rechannelWeights;
    private readonly float[] headWeights;
    private readonly float headBias;
    private readonly float headScale;

    private readonly float[] headRing;
    private readonly uint headMask;
    private readonly float[] headPrewarmedFrame;

    // Per-block scratch, MaxBlockFrames frames each.
    private readonly float[] layerIn;
    private readonly float[] headSum;
    private readonly float[] condition;
    private readonly float[] activation;

    // Frame counter shared by every ring. All ring sizes are powers of two, so
    // masking a uint that wraps at 2^32 stays consistent across the wrap.
    private uint frame;

    private sealed class Layer
    {
        public required int KernelSize;
        public required int Dilation;
        // Kernel taps: weight (tap k, input j, output o) at (k * channels + j) * stride + o.
        public required float[] ConvWeights;
        public required float[] ConvBias;
        public required float[] MixinWeights;
        // 1x1 residual: weight (input j, output o) at j * stride + o.
        public required float[] MixWeights;
        public required float[] MixBias;
        public required float[] Ring;
        public required uint Mask;
        public required float[] PrewarmedFrame;
    }

    public A2WaveNet(int channels, ReadOnlySpan<float> weights)
    {
        if (channels > TLanes.Count)
            throw new ArgumentOutOfRangeException(nameof(channels));
        this.channels = channels;
        var stride = TLanes.Count;
        var reader = new WeightReader(weights);

        rechannelWeights = new float[stride];
        for (var o = 0; o < channels; o++)
            rechannelWeights[o] = reader.Next();

        layers = new Layer[A2Shape.LayerCount];
        var lookbackTotal = 0;
        for (var i = 0; i < layers.Length; i++)
        {
            var k = A2Shape.KernelSizes[i];
            var d = A2Shape.Dilations[i];
            var lookback = (k - 1) * d;
            lookbackTotal += lookback;
            // The oldest tap plus the two frames of a pair being written.
            var ringFrames = (int)BitOperations.RoundUpToPowerOf2((uint)lookback + 2);
            var layer = new Layer
            {
                KernelSize = k,
                Dilation = d,
                ConvWeights = new float[k * channels * stride],
                ConvBias = new float[stride],
                MixinWeights = new float[stride],
                MixWeights = new float[channels * stride],
                MixBias = new float[stride],
                Ring = new float[ringFrames * stride],
                Mask = (uint)ringFrames - 1,
                PrewarmedFrame = new float[stride],
            };
            // Weight stream order (matches the reference loader):
            // conv: for out, for in, for tap; then conv bias; mixin; 1x1: for out, for in; 1x1 bias.
            for (var o = 0; o < channels; o++)
                for (var j = 0; j < channels; j++)
                    for (var tap = 0; tap < k; tap++)
                        layer.ConvWeights[(tap * channels + j) * stride + o] = reader.Next();
            for (var o = 0; o < channels; o++)
                layer.ConvBias[o] = reader.Next();
            for (var o = 0; o < channels; o++)
                layer.MixinWeights[o] = reader.Next();
            for (var o = 0; o < channels; o++)
                for (var j = 0; j < channels; j++)
                    layer.MixWeights[j * stride + o] = reader.Next();
            for (var o = 0; o < channels; o++)
                layer.MixBias[o] = reader.Next();
            layers[i] = layer;
        }

        headWeights = new float[A2Shape.HeadKernelSize * stride];
        for (var j = 0; j < channels; j++)
            for (var tap = 0; tap < A2Shape.HeadKernelSize; tap++)
                headWeights[tap * stride + j] = reader.Next();
        headBias = reader.Next();
        // The final weight is the head scale; it overrides the JSON "head_scale" field.
        headScale = reader.Next();
        reader.EnsureConsumed();

        headRing = new float[A2Shape.HeadKernelSize * stride];
        headMask = A2Shape.HeadKernelSize - 1;
        headPrewarmedFrame = new float[stride];

        layerIn = new float[MaxBlockFrames * stride];
        headSum = new float[MaxBlockFrames * stride];
        condition = new float[MaxBlockFrames];
        activation = new float[2 * stride];

        PrewarmSamples = 1 + lookbackTotal + A2Shape.HeadKernelSize - 1;
        PrewarmFromSilence();
    }

    /// <summary>
    /// Samples of silence the model needs to settle from an all-zero state: its
    /// receptive field. Matches the reference implementation's prewarm length.
    /// </summary>
    public int PrewarmSamples { get; }

    public override void Reset()
    {
        // After a full receptive field of silence every history holds one constant
        // frame, so the settled state is rebuilt by filling each ring with it.
        var stride = TLanes.Count;
        foreach (var layer in layers)
            FillRing(layer.Ring, layer.PrewarmedFrame, stride);
        FillRing(headRing, headPrewarmedFrame, stride);
    }

    public override void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        var n = input.Length;
        if (n > MaxBlockFrames)
            throw new ArgumentException($"At most {MaxBlockFrames} frames per call.", nameof(input));
        if (output.Length != n)
            throw new ArgumentException("Output must be the same length as input.", nameof(output));

        var stride = (nuint)TLanes.Count;
        ref var cond = ref MemoryMarshal.GetArrayDataReference(condition);
        ref var lin = ref MemoryMarshal.GetArrayDataReference(layerIn);
        var rechannel = TLanes.Load(ref MemoryMarshal.GetArrayDataReference(rechannelWeights), 0);
        for (var t = 0; t < n; t++)
        {
            var x = input[t];
            Unsafe.Add(ref cond, t) = x;
            TLanes.Store(TLanes.MultiplyAdd(rechannel, TLanes.Broadcast(x), default), ref lin, (nuint)t * stride);
        }
        Array.Clear(headSum, 0, n * TLanes.Count);

        foreach (var layer in layers)
            LayerForward(layer, n);

        HeadForward(output, n);
        frame += (uint)n;
    }

    private void LayerForward(Layer layer, int n)
    {
        var stride = (nuint)TLanes.Count;
        var c = channels;
        var kernelSize = layer.KernelSize;
        var dilation = (uint)layer.Dilation;
        var mask = layer.Mask;
        var oldestBack = (uint)(kernelSize - 1) * dilation;

        ref var ring = ref MemoryMarshal.GetArrayDataReference(layer.Ring);
        ref var convW = ref MemoryMarshal.GetArrayDataReference(layer.ConvWeights);
        ref var mixW = ref MemoryMarshal.GetArrayDataReference(layer.MixWeights);
        ref var lin = ref MemoryMarshal.GetArrayDataReference(layerIn);
        ref var hsum = ref MemoryMarshal.GetArrayDataReference(headSum);
        ref var cond = ref MemoryMarshal.GetArrayDataReference(condition);
        ref var act = ref MemoryMarshal.GetArrayDataReference(activation);

        var convBias = TLanes.Load(ref MemoryMarshal.GetArrayDataReference(layer.ConvBias), 0);
        var mixin = TLanes.Load(ref MemoryMarshal.GetArrayDataReference(layer.MixinWeights), 0);
        var mixBias = TLanes.Load(ref MemoryMarshal.GetArrayDataReference(layer.MixBias), 0);
        var slope = TLanes.Broadcast(A2Shape.LeakySlope);

        // Frames are processed in pairs: each weight is loaded once for both, and the
        // two frames' accumulators form independent FMA chains the CPU can overlap.
        // A frame only reads its own and earlier history, so writing both frames into
        // the ring first is safe.
        var t = 0;
        for (; t + 1 < n; t += 2)
        {
            var now = frame + (uint)t;
            var offset0 = (nuint)t * stride;
            var offset1 = offset0 + stride;
            var x0 = TLanes.Load(ref lin, offset0);
            var x1 = TLanes.Load(ref lin, offset1);
            TLanes.Store(x0, ref ring, (now & mask) * stride);
            TLanes.Store(x1, ref ring, ((now + 1) & mask) * stride);

            // Dilated causal convolution, oldest tap first.
            var z0 = TLanes.MultiplyAdd(mixin, TLanes.Broadcast(Unsafe.Add(ref cond, t)), convBias);
            var z1 = TLanes.MultiplyAdd(mixin, TLanes.Broadcast(Unsafe.Add(ref cond, t + 1)), convBias);
            nuint w = 0;
            var position = now - oldestBack;
            for (var tap = 0; tap < kernelSize; tap++, position += dilation)
            {
                ref var src0 = ref Unsafe.Add(ref ring, (position & mask) * stride);
                ref var src1 = ref Unsafe.Add(ref ring, ((position + 1) & mask) * stride);
                for (var j = 0; j < c; j++, w += stride)
                {
                    var weight = TLanes.Load(ref convW, w);
                    z0 = TLanes.MultiplyAdd(weight, TLanes.Broadcast(Unsafe.Add(ref src0, j)), z0);
                    z1 = TLanes.MultiplyAdd(weight, TLanes.Broadcast(Unsafe.Add(ref src1, j)), z1);
                }
            }

            var a0 = TLanes.LeakyRelu(z0, slope);
            var a1 = TLanes.LeakyRelu(z1, slope);
            TLanes.Store(TLanes.Add(TLanes.Load(ref hsum, offset0), a0), ref hsum, offset0);
            TLanes.Store(TLanes.Add(TLanes.Load(ref hsum, offset1), a1), ref hsum, offset1);

            // 1x1 residual mix back into the channel vector for the next layer.
            TLanes.Store(a0, ref act, 0);
            TLanes.Store(a1, ref act, stride);
            var r0 = TLanes.Add(x0, mixBias);
            var r1 = TLanes.Add(x1, mixBias);
            for (var j = 0; j < c; j++)
            {
                var weight = TLanes.Load(ref mixW, (nuint)j * stride);
                r0 = TLanes.MultiplyAdd(weight, TLanes.Broadcast(Unsafe.Add(ref act, j)), r0);
                r1 = TLanes.MultiplyAdd(weight, TLanes.Broadcast(Unsafe.Add(ref act, (nint)stride + j)), r1);
            }
            TLanes.Store(r0, ref lin, offset0);
            TLanes.Store(r1, ref lin, offset1);
        }

        // Odd frame left over.
        if (t < n)
        {
            var now = frame + (uint)t;
            var offset = (nuint)t * stride;
            var x = TLanes.Load(ref lin, offset);
            TLanes.Store(x, ref ring, (now & mask) * stride);

            var z = TLanes.MultiplyAdd(mixin, TLanes.Broadcast(Unsafe.Add(ref cond, t)), convBias);
            nuint w = 0;
            var position = now - oldestBack;
            for (var tap = 0; tap < kernelSize; tap++, position += dilation)
            {
                ref var src = ref Unsafe.Add(ref ring, (position & mask) * stride);
                for (var j = 0; j < c; j++, w += stride)
                    z = TLanes.MultiplyAdd(TLanes.Load(ref convW, w), TLanes.Broadcast(Unsafe.Add(ref src, j)), z);
            }

            var a = TLanes.LeakyRelu(z, slope);
            TLanes.Store(TLanes.Add(TLanes.Load(ref hsum, offset), a), ref hsum, offset);

            TLanes.Store(a, ref act, 0);
            var r = TLanes.Add(x, mixBias);
            for (var j = 0; j < c; j++)
                r = TLanes.MultiplyAdd(TLanes.Load(ref mixW, (nuint)j * stride), TLanes.Broadcast(Unsafe.Add(ref act, j)), r);
            TLanes.Store(r, ref lin, offset);
        }
    }

    private void HeadForward(Span<float> output, int n)
    {
        var stride = (nuint)TLanes.Count;
        ref var ring = ref MemoryMarshal.GetArrayDataReference(headRing);
        ref var hw = ref MemoryMarshal.GetArrayDataReference(headWeights);
        ref var hsum = ref MemoryMarshal.GetArrayDataReference(headSum);
        for (var t = 0; t < n; t++)
        {
            var now = frame + (uint)t;
            TLanes.Store(TLanes.Load(ref hsum, (nuint)t * stride), ref ring, (now & headMask) * stride);
            TLanes acc = default;
            var position = now - (A2Shape.HeadKernelSize - 1);
            for (nuint tap = 0; tap < A2Shape.HeadKernelSize; tap++, position++)
                acc = TLanes.MultiplyAdd(TLanes.Load(ref hw, tap * stride), TLanes.Load(ref ring, (position & headMask) * stride), acc);
            output[t] = (headBias + TLanes.Sum(acc)) * headScale;
        }
    }

    private void PrewarmFromSilence()
    {
        Span<float> silence = stackalloc float[MaxBlockFrames];
        silence.Clear();
        for (var remaining = PrewarmSamples; remaining > 0; remaining -= MaxBlockFrames)
        {
            var block = silence[..Math.Min(remaining, MaxBlockFrames)];
            Process(block, block);
            block.Clear();
        }

        var stride = TLanes.Count;
        var last = frame - 1;
        foreach (var layer in layers)
            Array.Copy(layer.Ring, (int)(last & layer.Mask) * stride, layer.PrewarmedFrame, 0, stride);
        Array.Copy(headRing, (int)(last & headMask) * stride, headPrewarmedFrame, 0, stride);
    }

    private static void FillRing(float[] ring, float[] frameValue, int stride)
    {
        var source = frameValue.AsSpan(0, stride);
        for (var i = 0; i < ring.Length; i += stride)
            source.CopyTo(ring.AsSpan(i, stride));
    }
}

/// <summary>Creates the A2 kernel with the widest SIMD layout the hardware runs natively.</summary>
internal static class A2WaveNet
{
    public static NamDsp Create(int channels, ReadOnlySpan<float> weights) => channels switch
    {
        <= 4 => new A2WaveNet<Lanes4>(channels, weights),
        _ when Vector256.IsHardwareAccelerated => new A2WaveNet<Lanes8>(channels, weights),
        _ => new A2WaveNet<Lanes8x2>(channels, weights),
    };
}
