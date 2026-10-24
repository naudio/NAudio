// Ported to C# for NAudio from NeuralAmpModelerCore
// (https://github.com/sdatkinson/NeuralAmpModelerCore), NAM/wavenet/model.cpp.
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
using System.Numerics.Tensors;

namespace NAudio.Nam.Internal;

/// <summary>
/// The general NAM WaveNet (A1 and custom architectures): a chain of layer arrays,
/// each a stack of dilated causal convolutions with a head that accumulates every
/// layer's activations. Each layer array after the first takes the previous one's
/// residual stream as input and its head output as the starting head sum.
/// </summary>
/// <remarks>
/// Channel vectors are stored per frame, padded to a whole number of
/// <see cref="Vector{T}"/> lanes so every multiply-accumulate is a vector operation
/// (see <see cref="WaveNetKernels"/>); padded lanes always meet zero weights, so they
/// never affect the output. Each layer runs over a whole block: convolution, then the
/// activation over the block with <see cref="TensorPrimitives"/>, then the 1x1 mix.
/// </remarks>
internal sealed class WaveNet : NamDsp
{
    private readonly LayerArray[] arrays;
    private readonly float headScale;
    private readonly float[] condition = new float[MaxBlockFrames];
    private uint frame;

    public WaveNet(WaveNetSpec spec, ReadOnlySpan<float> weights)
    {
        var reader = new WeightReader(weights);
        arrays = new LayerArray[spec.LayerArrays.Count];
        var receptiveField = 1;
        for (var i = 0; i < arrays.Length; i++)
        {
            arrays[i] = new LayerArray(spec.LayerArrays[i], ref reader);
            receptiveField += arrays[i].ReceptiveField;
        }
        headScale = reader.Next();
        reader.EnsureConsumed();

        PrewarmSamples = receptiveField;
        Span<float> silence = stackalloc float[MaxBlockFrames];
        for (var remaining = PrewarmSamples; remaining > 0; remaining -= MaxBlockFrames)
        {
            silence.Clear();
            var block = silence[..Math.Min(remaining, MaxBlockFrames)];
            Process(block, block);
        }
        foreach (var array in arrays)
            array.CacheSettledState(frame - 1);
    }

    /// <summary>Samples of silence the model needs to settle; matches NeuralAmpModelerCore's prewarm.</summary>
    public int PrewarmSamples { get; }

    public override void Reset()
    {
        foreach (var array in arrays)
            array.RestoreSettledState();
    }

    public override void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        var n = input.Length;
        if (n > MaxBlockFrames)
            throw new ArgumentException($"At most {MaxBlockFrames} frames per call.", nameof(input));
        if (output.Length != n)
            throw new ArgumentException("Output must be the same length as input.", nameof(output));

        input.CopyTo(condition);
        var cond = condition.AsSpan(0, n);
        for (var a = 0; a < arrays.Length; a++)
        {
            if (a == 0)
                arrays[a].Process(cond, inputStride: 1, previousHead: default, cond, n, frame);
            else
                arrays[a].Process(arrays[a - 1].LayerOutput(n), arrays[a - 1].ChannelStride, arrays[a - 1].HeadOutput(n), cond, n, frame);
        }

        var last = arrays[^1];
        var head = last.HeadOutput(n);
        for (var t = 0; t < n; t++)
            output[t] = headScale * head[t * last.HeadStride];
        frame += (uint)n;
    }

    private static int Pad(int channels) => WaveNetKernels.Pad(channels);

    private sealed class LayerArray
    {
        private readonly int inputSize;
        private readonly int channels;
        private readonly int bottleneck;
        private readonly float[] rechannel;   // input j -> channel column at j * ChannelStride
        private readonly Layer[] layers;
        private readonly CausalConv head;
        private readonly float[] layerIn;     // residual stream, MaxBlockFrames x ChannelStride
        private readonly float[] z;           // conv output, MaxBlockFrames x widest layer stride
        private readonly float[] headSum;     // MaxBlockFrames x bottleneck stride
        private readonly float[] headOut;     // MaxBlockFrames x HeadStride

        public LayerArray(WaveNetSpec.LayerArraySpec spec, ref WeightReader reader)
        {
            inputSize = spec.InputSize;
            channels = spec.Channels;
            bottleneck = spec.Bottleneck;
            ChannelStride = Pad(channels);
            var bottleneckStride = Pad(bottleneck);
            HeadStride = Pad(spec.HeadSize);

            rechannel = new float[inputSize * ChannelStride];
            for (var o = 0; o < channels; o++)
                for (var j = 0; j < inputSize; j++)
                    rechannel[j * ChannelStride + o] = reader.Next();

            layers = new Layer[spec.Dilations.Length];
            var widest = bottleneckStride;
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = new Layer(spec, i, ChannelStride, bottleneckStride, ref reader);
                widest = Math.Max(widest, layers[i].ZStride);
                ReceptiveField += (spec.KernelSizes[i] - 1) * spec.Dilations[i];
            }

            head = new CausalConv(bottleneck, bottleneckStride, HeadStride, spec.HeadKernelSize, spec.HeadDilation);
            head.ReadWeights(ref reader, spec.HeadSize, o => o, spec.HeadBias);
            ReceptiveField += (spec.HeadKernelSize - 1) * spec.HeadDilation;

            layerIn = new float[MaxBlockFrames * ChannelStride];
            z = new float[MaxBlockFrames * widest];
            headSum = new float[MaxBlockFrames * bottleneckStride];
            headOut = new float[MaxBlockFrames * HeadStride];
        }

        public int ChannelStride { get; }
        public int HeadStride { get; }
        public int ReceptiveField { get; }

        public ReadOnlySpan<float> LayerOutput(int n) => layerIn.AsSpan(0, n * ChannelStride);
        public ReadOnlySpan<float> HeadOutput(int n) => headOut.AsSpan(0, n * HeadStride);

        public void Process(ReadOnlySpan<float> input, int inputStride, ReadOnlySpan<float> previousHead, ReadOnlySpan<float> cond, int n, uint frame)
        {
            var cs = ChannelStride;
            var stream = layerIn.AsSpan(0, n * cs);
            stream.Clear();
            WaveNetKernels.MatVecAccumulate(rechannel, inputSize, input, inputStride, stream, cs, n);

            var bs = Pad(bottleneck);
            var sum = headSum.AsSpan(0, n * bs);
            if (previousHead.IsEmpty)
                sum.Clear();
            else
                previousHead.CopyTo(sum);

            foreach (var layer in layers)
                layer.Process(stream, z, sum, cond, n, frame);

            head.Forward(sum, headOut, n, frame);
        }

        public void CacheSettledState(uint lastFrame)
        {
            foreach (var layer in layers)
                layer.Conv.CacheSettledState(lastFrame);
            head.CacheSettledState(lastFrame);
        }

        public void RestoreSettledState()
        {
            foreach (var layer in layers)
                layer.Conv.RestoreSettledState();
            head.RestoreSettledState();
        }
    }

    private sealed class Layer
    {
        private readonly int channelStride;
        private readonly int bottleneck;
        private readonly int bottleneckStride;
        private readonly float[] mixin;            // condition -> z, ZStride wide
        private readonly Activation activation;
        private readonly Activation? gate;
        private readonly float[]? mixWeights;      // bottleneck j -> channel column at j * channelStride
        private readonly float[]? mixBias;

        public Layer(WaveNetSpec.LayerArraySpec spec, int index, int channelStride, int bottleneckStride, ref WeightReader reader)
        {
            this.channelStride = channelStride;
            this.bottleneckStride = bottleneckStride;
            bottleneck = spec.Bottleneck;
            activation = spec.Activations[index];
            gate = spec.GateActivations[index];
            var zRows = gate != null ? 2 * bottleneck : bottleneck;
            // A gated layer's z holds the activation half then the gate half, each padded.
            ZStride = gate != null ? 2 * bottleneckStride : bottleneckStride;
            int Row(int o) => o < bottleneck ? o : bottleneckStride + o - bottleneck;

            Conv = new CausalConv(spec.Channels, channelStride, ZStride, spec.KernelSizes[index], spec.Dilations[index]);
            Conv.ReadWeights(ref reader, zRows, Row, bias: true);

            mixin = new float[ZStride];
            for (var o = 0; o < zRows; o++)
                mixin[Row(o)] = reader.Next();

            if (spec.Layer1x1)
            {
                mixWeights = new float[bottleneck * channelStride];
                mixBias = new float[channelStride];
                for (var o = 0; o < spec.Channels; o++)
                    for (var j = 0; j < bottleneck; j++)
                        mixWeights[j * channelStride + o] = reader.Next();
                for (var o = 0; o < spec.Channels; o++)
                    mixBias[o] = reader.Next();
            }
        }

        public CausalConv Conv { get; }
        public int ZStride { get; }

        public void Process(Span<float> stream, float[] zBuffer, Span<float> headSum, ReadOnlySpan<float> cond, int n, uint frame)
        {
            var zs = ZStride;
            var bs = bottleneckStride;
            var z = zBuffer.AsSpan(0, n * zs);
            Conv.Forward(stream, z, n, frame);
            WaveNetKernels.MatVecAccumulate(mixin, 1, cond, 1, z, zs, n);

            if (gate == null)
            {
                activation.Apply(z);
                TensorPrimitives.Add(headSum, z, headSum);
            }
            else
            {
                for (var t = 0; t < n; t++)
                {
                    var top = z.Slice(t * zs, bs);
                    var bottom = z.Slice(t * zs + bs, bs);
                    activation.Apply(top);
                    gate.Apply(bottom);
                    TensorPrimitives.Multiply(top, bottom, top);
                    var h = headSum.Slice(t * bs, bs);
                    TensorPrimitives.Add(h, top, h);
                }
            }

            if (mixWeights == null)
                return; // identity residual
            var cs = channelStride;
            for (var t = 0; t < n; t++)
            {
                var target = stream.Slice(t * cs, cs);
                TensorPrimitives.Add(target, mixBias, target);
            }
            WaveNetKernels.MatVecAccumulate(mixWeights, bottleneck, z, zs, stream, cs, n);
        }
    }

    /// <summary>
    /// A causal dilated convolution over frames of padded channel vectors, with its
    /// input history in a power-of-two ring buffer indexed by the shared frame counter.
    /// </summary>
    private sealed class CausalConv
    {
        private readonly int inChannels;
        private readonly int inStride;
        private readonly int outStride;
        private readonly int kernelSize;
        private readonly int dilation;
        private readonly float[] weights;   // (tap k, input j) -> output column at (k * inChannels + j) * outStride
        private readonly float[] bias;
        private readonly float[] ring;
        private readonly uint mask;
        private readonly float[] settledFrame;

        public CausalConv(int inChannels, int inStride, int outStride, int kernelSize, int dilation)
        {
            this.inChannels = inChannels;
            this.inStride = inStride;
            this.outStride = outStride;
            this.kernelSize = kernelSize;
            this.dilation = dilation;
            weights = new float[kernelSize * inChannels * outStride];
            bias = new float[outStride];
            // A whole block is written before it is read, so the ring holds the
            // lookback plus a block.
            var ringFrames = (int)BitOperations.RoundUpToPowerOf2((uint)((kernelSize - 1) * dilation + MaxBlockFrames));
            ring = new float[ringFrames * inStride];
            mask = (uint)ringFrames - 1;
            settledFrame = new float[inStride];
        }

        /// <summary>Reads weights in the reference order: for each output, each input, each tap; then the bias.</summary>
        public void ReadWeights(ref WeightReader reader, int outputs, Func<int, int> row, bool bias)
        {
            for (var o = 0; o < outputs; o++)
                for (var j = 0; j < inChannels; j++)
                    for (var k = 0; k < kernelSize; k++)
                        weights[(k * inChannels + j) * outStride + row(o)] = reader.Next();
            if (bias)
            {
                for (var o = 0; o < outputs; o++)
                    this.bias[row(o)] = reader.Next();
            }
        }

        /// <summary>Writes the block's input frames into the history, then computes bias + convolution for each frame.</summary>
        public void Forward(ReadOnlySpan<float> input, Span<float> output, int n, uint frame)
        {
            for (var t = 0; t < n; t++)
                input.Slice(t * inStride, inStride).CopyTo(ring.AsSpan((int)((frame + (uint)t) & mask) * inStride, inStride));
            WaveNetKernels.Convolve(weights, bias, inChannels, kernelSize, dilation, ring, inStride, mask, frame, output, outStride, n);
        }

        public void CacheSettledState(uint lastFrame) =>
            Array.Copy(ring, (int)(lastFrame & mask) * inStride, settledFrame, 0, inStride);

        public void RestoreSettledState()
        {
            for (var i = 0; i < ring.Length; i += inStride)
                settledFrame.CopyTo(ring, i);
        }
    }
}
