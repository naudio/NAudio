using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace NAudio.Nam.Internal;

/// <summary>
/// A fixed-width group of float lanes holding one frame of a channel vector. The A2
/// kernel is written once against this interface and specialised by the JIT for each
/// struct below, so there is no per-call dispatch on the hot path.
/// </summary>
internal interface ISimdLanes<TSelf> where TSelf : struct, ISimdLanes<TSelf>
{
    /// <summary>Number of float lanes (the padded per-frame stride).</summary>
    static abstract int Count { get; }

    static abstract TSelf Load(ref float source, nuint offset);

    static abstract void Store(TSelf value, ref float destination, nuint offset);

    static abstract TSelf Broadcast(float value);

    /// <summary>Returns <c>a * b + c</c>, fused where the hardware supports it.</summary>
    static abstract TSelf MultiplyAdd(TSelf a, TSelf b, TSelf c);

    static abstract TSelf Add(TSelf a, TSelf b);

    /// <summary>LeakyReLU for a slope in (0, 1): <c>max(x, x * slope)</c>.</summary>
    static abstract TSelf LeakyRelu(TSelf value, TSelf slope);

    static abstract float Sum(TSelf value);
}

/// <summary>Four lanes in one 128-bit vector (A2-Lite: 3 channels padded to 4).</summary>
internal readonly struct Lanes4(Vector128<float> v) : ISimdLanes<Lanes4>
{
    private readonly Vector128<float> v = v;

    public static int Count => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes4 Load(ref float source, nuint offset) => new(Vector128.LoadUnsafe(ref source, offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Store(Lanes4 value, ref float destination, nuint offset) => value.v.StoreUnsafe(ref destination, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes4 Broadcast(float value) => new(Vector128.Create(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes4 MultiplyAdd(Lanes4 a, Lanes4 b, Lanes4 c) => new(Vector128.MultiplyAddEstimate(a.v, b.v, c.v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes4 Add(Lanes4 a, Lanes4 b) => new(a.v + b.v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes4 LeakyRelu(Lanes4 value, Lanes4 slope) => new(Vector128.Max(value.v, value.v * slope.v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sum(Lanes4 value) => Vector128.Sum(value.v);
}

/// <summary>Eight lanes in one 256-bit vector (A2-Full where AVX is available).</summary>
internal readonly struct Lanes8(Vector256<float> v) : ISimdLanes<Lanes8>
{
    private readonly Vector256<float> v = v;

    public static int Count => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8 Load(ref float source, nuint offset) => new(Vector256.LoadUnsafe(ref source, offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Store(Lanes8 value, ref float destination, nuint offset) => value.v.StoreUnsafe(ref destination, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8 Broadcast(float value) => new(Vector256.Create(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8 MultiplyAdd(Lanes8 a, Lanes8 b, Lanes8 c) => new(Vector256.MultiplyAddEstimate(a.v, b.v, c.v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8 Add(Lanes8 a, Lanes8 b) => new(a.v + b.v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8 LeakyRelu(Lanes8 value, Lanes8 slope) => new(Vector256.Max(value.v, value.v * slope.v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sum(Lanes8 value) => Vector256.Sum(value.v);
}

/// <summary>
/// Eight lanes as a pair of 128-bit vectors (A2-Full on hardware without 256-bit
/// vectors, e.g. Arm64 NEON, where <see cref="Vector256{T}"/> is software-emulated).
/// </summary>
internal readonly struct Lanes8x2(Vector128<float> lo, Vector128<float> hi) : ISimdLanes<Lanes8x2>
{
    private readonly Vector128<float> lo = lo;
    private readonly Vector128<float> hi = hi;

    public static int Count => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8x2 Load(ref float source, nuint offset) =>
        new(Vector128.LoadUnsafe(ref source, offset), Vector128.LoadUnsafe(ref source, offset + 4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Store(Lanes8x2 value, ref float destination, nuint offset)
    {
        value.lo.StoreUnsafe(ref destination, offset);
        value.hi.StoreUnsafe(ref destination, offset + 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8x2 Broadcast(float value)
    {
        var b = Vector128.Create(value);
        return new(b, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8x2 MultiplyAdd(Lanes8x2 a, Lanes8x2 b, Lanes8x2 c) =>
        new(Vector128.MultiplyAddEstimate(a.lo, b.lo, c.lo), Vector128.MultiplyAddEstimate(a.hi, b.hi, c.hi));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8x2 Add(Lanes8x2 a, Lanes8x2 b) => new(a.lo + b.lo, a.hi + b.hi);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Lanes8x2 LeakyRelu(Lanes8x2 value, Lanes8x2 slope) =>
        new(Vector128.Max(value.lo, value.lo * slope.lo), Vector128.Max(value.hi, value.hi * slope.hi));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sum(Lanes8x2 value) => Vector128.Sum(value.lo + value.hi);
}
