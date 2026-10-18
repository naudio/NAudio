// Ported to C# for NAudio from NeuralAmpModelerCore
// (https://github.com/sdatkinson/NeuralAmpModelerCore), NAM/lstm.cpp.
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
using System.Runtime.InteropServices;
using System.Text.Json;

namespace NAudio.Nam.Internal;

/// <summary>
/// A NAM LSTM model: stacked LSTM cells followed by a linear head. Every sample
/// depends on the state left by the previous one, so it is processed sample by
/// sample; each cell's gate computation is a vectorised matrix-vector product.
/// </summary>
internal sealed class Lstm : NamDsp
{
    private readonly Cell[] cells;
    private readonly float[] headWeights;
    private readonly float headBias;

    public Lstm(LstmSpec spec, ReadOnlySpan<float> weights, int? sampleRate)
    {
        var reader = new WeightReader(weights);
        cells = new Cell[spec.NumLayers];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = new Cell(i == 0 ? spec.InputSize : spec.HiddenSize, spec.HiddenSize, ref reader);
        headWeights = new float[spec.HiddenSize];
        for (var h = 0; h < headWeights.Length; h++)
            headWeights[h] = reader.Next();
        headBias = reader.Next();
        reader.EnsureConsumed();

        // NeuralAmpModelerCore prewarms an LSTM with half a second of silence.
        PrewarmSamples = Math.Max(1, (int)(0.5 * (sampleRate ?? 0)));
        Span<float> silence = stackalloc float[MaxBlockFrames];
        for (var remaining = PrewarmSamples; remaining > 0; remaining -= MaxBlockFrames)
        {
            silence.Clear();
            var block = silence[..Math.Min(remaining, MaxBlockFrames)];
            Process(block, block);
        }
        foreach (var cell in cells)
            cell.CacheSettledState();
    }

    public int PrewarmSamples { get; }

    public override void Reset()
    {
        foreach (var cell in cells)
            cell.RestoreSettledState();
    }

    public override void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (output.Length != input.Length)
            throw new ArgumentException("Output must be the same length as input.", nameof(output));
        for (var t = 0; t < input.Length; t++)
        {
            var x = input[t];
            ReadOnlySpan<float> layerInput = new(ref x);
            foreach (var cell in cells)
                layerInput = cell.Step(layerInput);
            output[t] = TensorPrimitives.Dot(headWeights, layerInput) + headBias;
        }
    }

    private sealed class Cell
    {
        private readonly int inputSize;
        private readonly int hidden;
        private readonly int gateStride;   // 4 * hidden, padded to whole vectors
        private readonly float[] columns;  // column j of the (4H x (I + H)) weight matrix at j * gateStride
        private readonly float[] bias;
        private readonly float[] xh;       // [input | hidden state]
        private readonly float[] cellState;
        private readonly float[] gates;    // i, f, g, o blocks of hidden each
        private readonly float[] scratch;
        private readonly float[] settledXh;
        private readonly float[] settledCell;

        public Cell(int inputSize, int hidden, ref WeightReader reader)
        {
            this.inputSize = inputSize;
            this.hidden = hidden;
            gateStride = (4 * hidden + Vector<float>.Count - 1) / Vector<float>.Count * Vector<float>.Count;
            var cols = inputSize + hidden;
            columns = new float[cols * gateStride];
            bias = new float[gateStride];
            xh = new float[cols];
            cellState = new float[hidden];
            gates = new float[gateStride];
            scratch = new float[hidden];
            settledXh = new float[cols];
            settledCell = new float[hidden];

            // Reference order: weight matrix row by row, bias, initial hidden state, initial cell state.
            for (var row = 0; row < 4 * hidden; row++)
                for (var j = 0; j < cols; j++)
                    columns[j * gateStride + row] = reader.Next();
            for (var row = 0; row < 4 * hidden; row++)
                bias[row] = reader.Next();
            for (var h = 0; h < hidden; h++)
                xh[inputSize + h] = reader.Next();
            for (var h = 0; h < hidden; h++)
                cellState[h] = reader.Next();
        }

        /// <summary>Advances one sample and returns the new hidden state.</summary>
        public ReadOnlySpan<float> Step(ReadOnlySpan<float> input)
        {
            input.CopyTo(xh);

            // gates = bias + W · [input | hidden], one weight column at a time.
            ref var w = ref MemoryMarshal.GetArrayDataReference(columns);
            ref var g = ref MemoryMarshal.GetArrayDataReference(gates);
            ref var b = ref MemoryMarshal.GetArrayDataReference(bias);
            var lanes = (nuint)Vector<float>.Count;
            var stride = (nuint)gateStride;
            for (nuint v = 0; v < stride; v += lanes)
            {
                var acc = Vector.LoadUnsafe(ref b, v);
                nuint c = v;
                for (var j = 0; j < xh.Length; j++, c += stride)
                    acc = Vector.MultiplyAddEstimate(Vector.LoadUnsafe(ref w, c), new Vector<float>(xh[j]), acc);
                acc.StoreUnsafe(ref g, v);
            }

            var h = hidden;
            var inputGate = gates.AsSpan(0, h);
            var forgetGate = gates.AsSpan(h, h);
            var candidate = gates.AsSpan(2 * h, h);
            var outputGate = gates.AsSpan(3 * h, h);
            TensorPrimitives.Sigmoid(gates.AsSpan(0, 2 * h), gates.AsSpan(0, 2 * h)); // input and forget gates
            TensorPrimitives.Tanh(candidate, candidate);
            TensorPrimitives.Sigmoid(outputGate, outputGate);

            // c = f * c + i * g;  h = o * tanh(c)
            TensorPrimitives.Multiply(inputGate, candidate, scratch);
            TensorPrimitives.MultiplyAdd(forgetGate, cellState, scratch, cellState);
            TensorPrimitives.Tanh(cellState, scratch);
            var hiddenState = xh.AsSpan(inputSize, h);
            TensorPrimitives.Multiply(outputGate, scratch, hiddenState);
            return hiddenState;
        }

        public void CacheSettledState()
        {
            xh.CopyTo(settledXh, 0);
            cellState.CopyTo(settledCell, 0);
        }

        public void RestoreSettledState()
        {
            settledXh.CopyTo(xh, 0);
            settledCell.CopyTo(cellState, 0);
        }
    }
}

/// <summary>A parsed NAM LSTM configuration.</summary>
internal sealed class LstmSpec
{
    public required int InputSize { get; init; }
    public required int HiddenSize { get; init; }
    public required int NumLayers { get; init; }

    public int WeightCount
    {
        get
        {
            var total = 0;
            for (var l = 0; l < NumLayers; l++)
            {
                var inputs = l == 0 ? InputSize : HiddenSize;
                total += 4 * HiddenSize * (inputs + HiddenSize) + 4 * HiddenSize + 2 * HiddenSize;
            }
            return total + HiddenSize + 1;
        }
    }

    public static LstmSpec Parse(JsonElement config)
    {
        static int Get(JsonElement c, string name) =>
            c.ValueKind == JsonValueKind.Object && c.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)
                ? i
                : throw new FormatException($"The LSTM config is missing '{name}'.");

        var spec = new LstmSpec
        {
            InputSize = Get(config, "input_size"),
            HiddenSize = Get(config, "hidden_size"),
            NumLayers = Get(config, "num_layers"),
        };
        if (spec.InputSize != 1)
            throw new NotSupportedException("Only single-input LSTM models are supported.");
        if (spec.HiddenSize < 1 || spec.NumLayers < 1)
            throw new NotSupportedException("LSTM models need at least one layer and one hidden unit.");
        return spec;
    }
}
