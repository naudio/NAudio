using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.Nam.Internal;

/// <summary>
/// Matrix-vector kernels for the general WaveNet. A channel vector is 1, 2, 4 or a
/// multiple of 4 <see cref="Vector{T}"/>s wide (see <see cref="Pad"/>); for the
/// common widths the kernels keep each output frame in registers across every input
/// and tap, and work on two frames at once so their multiply-add chains overlap. A
/// generic struct per width lets the JIT specialise one kernel body for each.
/// </summary>
internal static class WaveNetKernels
{
    /// <summary>
    /// Rounds a channel count up to a vector-group width: 1, 2 or 4 vectors, or a
    /// multiple of 4. Padded lanes always meet zero weights.
    /// </summary>
    public static int Pad(int channels)
    {
        var lanes = Vector<float>.Count;
        var vectors = (channels + lanes - 1) / lanes;
        vectors = vectors <= 2 ? vectors : (vectors + 3) / 4 * 4;
        return vectors * lanes;
    }

    /// <summary>
    /// output[t] += Σ_j column_j · input[t, j] for n frames. Column j of
    /// <paramref name="weights"/> starts at <c>j * outStride</c>.
    /// </summary>
    public static void MatVecAccumulate(ReadOnlySpan<float> weights, int inputs, ReadOnlySpan<float> input, int inStride, Span<float> output, int outStride, int n)
    {
        switch (outStride / Vector<float>.Count)
        {
            case 1: MatVec<X1>(weights, inputs, input, inStride, output, outStride, n); break;
            case 2: MatVec<X2>(weights, inputs, input, inStride, output, outStride, n); break;
            case 4: MatVec<X4>(weights, inputs, input, inStride, output, outStride, n); break;
            default:
                for (var g = 0; g < outStride; g += X4.Width)
                    MatVecSlice(weights, inputs, input, inStride, output, outStride, n, g);
                break;
        }
    }

    /// <summary>
    /// output[t] = bias + Σ_k Σ_j W[k, j] · ring[frame(t, k), j]: a causal dilated
    /// convolution reading its input from a ring of frames. Column (k, j) of
    /// <paramref name="weights"/> starts at <c>(k * inputs + j) * outStride</c>.
    /// </summary>
    public static void Convolve(ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, int inputs, int kernelSize, int dilation,
        ReadOnlySpan<float> ring, int inStride, uint mask, uint frame, Span<float> output, int outStride, int n)
    {
        switch (outStride / Vector<float>.Count)
        {
            case 1: Conv<X1>(weights, bias, inputs, kernelSize, dilation, ring, inStride, mask, frame, output, outStride, n, 0); break;
            case 2: Conv<X2>(weights, bias, inputs, kernelSize, dilation, ring, inStride, mask, frame, output, outStride, n, 0); break;
            case 4: Conv<X4>(weights, bias, inputs, kernelSize, dilation, ring, inStride, mask, frame, output, outStride, n, 0); break;
            default:
                for (var g = 0; g < outStride; g += X4.Width)
                    Conv<X4>(weights, bias, inputs, kernelSize, dilation, ring, inStride, mask, frame, output, outStride, n, g);
                break;
        }
    }

    private static void MatVec<T>(ReadOnlySpan<float> weights, int inputs, ReadOnlySpan<float> input, int inStride, Span<float> output, int outStride, int n)
        where T : struct, IVectorGroup<T>
    {
        ref var w = ref MemoryMarshal.GetReference(weights);
        ref var x = ref MemoryMarshal.GetReference(input);
        ref var o = ref MemoryMarshal.GetReference(output);
        var os = (nuint)outStride;
        var t = 0;
        for (; t + 1 < n; t += 2)
        {
            var o0 = (nuint)t * os;
            var o1 = o0 + os;
            var acc0 = T.Load(ref o, o0);
            var acc1 = T.Load(ref o, o1);
            ref var x0 = ref Unsafe.Add(ref x, t * inStride);
            ref var x1 = ref Unsafe.Add(ref x0, inStride);
            nuint c = 0;
            for (var j = 0; j < inputs; j++, c += os)
            {
                var column = T.Load(ref w, c);
                acc0 = T.MultiplyAdd(column, new Vector<float>(Unsafe.Add(ref x0, j)), acc0);
                acc1 = T.MultiplyAdd(column, new Vector<float>(Unsafe.Add(ref x1, j)), acc1);
            }
            T.Store(acc0, ref o, o0);
            T.Store(acc1, ref o, o1);
        }
        if (t < n)
        {
            var o0 = (nuint)t * os;
            var acc = T.Load(ref o, o0);
            ref var x0 = ref Unsafe.Add(ref x, t * inStride);
            nuint c = 0;
            for (var j = 0; j < inputs; j++, c += os)
                acc = T.MultiplyAdd(T.Load(ref w, c), new Vector<float>(Unsafe.Add(ref x0, j)), acc);
            T.Store(acc, ref o, o0);
        }
    }

    // One X4-wide slice of a matrix wider than 4 vectors.
    private static void MatVecSlice(ReadOnlySpan<float> weights, int inputs, ReadOnlySpan<float> input, int inStride, Span<float> output, int outStride, int n, int offset)
    {
        for (var t = 0; t < n; t++)
        {
            var o0 = t * outStride + offset;
            var acc = X4.Load(ref MemoryMarshal.GetReference(output), (nuint)o0);
            for (var j = 0; j < inputs; j++)
                acc = X4.MultiplyAdd(X4.Load(ref MemoryMarshal.GetReference(weights), (nuint)(j * outStride + offset)), new Vector<float>(input[t * inStride + j]), acc);
            X4.Store(acc, ref MemoryMarshal.GetReference(output), (nuint)o0);
        }
    }

    private static void Conv<T>(ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, int inputs, int kernelSize, int dilation,
        ReadOnlySpan<float> ring, int inStride, uint mask, uint frame, Span<float> output, int outStride, int n, int offset)
        where T : struct, IVectorGroup<T>
    {
        ref var w = ref Unsafe.Add(ref MemoryMarshal.GetReference(weights), offset);
        ref var r = ref MemoryMarshal.GetReference(ring);
        ref var o = ref Unsafe.Add(ref MemoryMarshal.GetReference(output), offset);
        var b = T.Load(ref Unsafe.Add(ref MemoryMarshal.GetReference(bias), offset), 0);
        var os = (nuint)outStride;
        var oldest = (uint)((kernelSize - 1) * dilation);
        var t = 0;
        for (; t + 1 < n; t += 2)
        {
            var acc0 = b;
            var acc1 = b;
            var position = frame + (uint)t - oldest;
            nuint c = 0;
            for (var k = 0; k < kernelSize; k++, position += (uint)dilation)
            {
                ref var x0 = ref Unsafe.Add(ref r, (nint)((position & mask) * (uint)inStride));
                ref var x1 = ref Unsafe.Add(ref r, (nint)(((position + 1) & mask) * (uint)inStride));
                for (var j = 0; j < inputs; j++, c += os)
                {
                    var column = T.Load(ref w, c);
                    acc0 = T.MultiplyAdd(column, new Vector<float>(Unsafe.Add(ref x0, j)), acc0);
                    acc1 = T.MultiplyAdd(column, new Vector<float>(Unsafe.Add(ref x1, j)), acc1);
                }
            }
            T.Store(acc0, ref o, (nuint)t * os);
            T.Store(acc1, ref o, (nuint)(t + 1) * os);
        }
        if (t < n)
        {
            var acc = b;
            var position = frame + (uint)t - oldest;
            nuint c = 0;
            for (var k = 0; k < kernelSize; k++, position += (uint)dilation)
            {
                ref var x0 = ref Unsafe.Add(ref r, (nint)((position & mask) * (uint)inStride));
                for (var j = 0; j < inputs; j++, c += os)
                    acc = T.MultiplyAdd(T.Load(ref w, c), new Vector<float>(Unsafe.Add(ref x0, j)), acc);
            }
            T.Store(acc, ref o, (nuint)t * os);
        }
    }

    private interface IVectorGroup<TSelf> where TSelf : struct, IVectorGroup<TSelf>
    {
        static abstract TSelf Load(ref float source, nuint offset);
        static abstract void Store(TSelf value, ref float destination, nuint offset);
        /// <summary>Returns <c>a * x + c</c> with <paramref name="x"/> broadcast to every lane.</summary>
        static abstract TSelf MultiplyAdd(TSelf a, Vector<float> x, TSelf c);
    }

    private readonly struct X1(Vector<float> a) : IVectorGroup<X1>
    {
        private readonly Vector<float> a = a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X1 Load(ref float s, nuint i) => new(Vector.LoadUnsafe(ref s, i));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(X1 v, ref float d, nuint i) => v.a.StoreUnsafe(ref d, i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X1 MultiplyAdd(X1 a, Vector<float> x, X1 c) => new(Vector.MultiplyAddEstimate(a.a, x, c.a));
    }

    private readonly struct X2(Vector<float> a, Vector<float> b) : IVectorGroup<X2>
    {
        private readonly Vector<float> a = a, b = b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X2 Load(ref float s, nuint i) =>
            new(Vector.LoadUnsafe(ref s, i), Vector.LoadUnsafe(ref s, i + (nuint)Vector<float>.Count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(X2 v, ref float d, nuint i)
        {
            v.a.StoreUnsafe(ref d, i);
            v.b.StoreUnsafe(ref d, i + (nuint)Vector<float>.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X2 MultiplyAdd(X2 a, Vector<float> x, X2 c) =>
            new(Vector.MultiplyAddEstimate(a.a, x, c.a), Vector.MultiplyAddEstimate(a.b, x, c.b));
    }

    private readonly struct X4(Vector<float> a, Vector<float> b, Vector<float> c, Vector<float> d) : IVectorGroup<X4>
    {
        public static int Width => 4 * Vector<float>.Count;

        private readonly Vector<float> a = a, b = b, c = c, d = d;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X4 Load(ref float s, nuint i)
        {
            var l = (nuint)Vector<float>.Count;
            return new(Vector.LoadUnsafe(ref s, i), Vector.LoadUnsafe(ref s, i + l), Vector.LoadUnsafe(ref s, i + 2 * l), Vector.LoadUnsafe(ref s, i + 3 * l));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(X4 v, ref float dst, nuint i)
        {
            var l = (nuint)Vector<float>.Count;
            v.a.StoreUnsafe(ref dst, i);
            v.b.StoreUnsafe(ref dst, i + l);
            v.c.StoreUnsafe(ref dst, i + 2 * l);
            v.d.StoreUnsafe(ref dst, i + 3 * l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X4 MultiplyAdd(X4 a, Vector<float> x, X4 c) =>
            new(Vector.MultiplyAddEstimate(a.a, x, c.a), Vector.MultiplyAddEstimate(a.b, x, c.b),
                Vector.MultiplyAddEstimate(a.c, x, c.c), Vector.MultiplyAddEstimate(a.d, x, c.d));
    }
}
