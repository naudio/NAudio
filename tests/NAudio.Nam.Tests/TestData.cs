using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudio.Nam.Tests;

/// <summary>
/// Access to the golden files in TestData/ (see TestData/README.md for how they
/// were produced).
/// </summary>
internal static class TestData
{
    public static string Path(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", name);

    public static NamModel LoadA2() => NamModel.Load(Path("A2.nam"));

    public static float[] ReadFloats(string name)
    {
        var bytes = File.ReadAllBytes(Path(name));
        return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
    }

    public static float[] Input => ReadFloats("input.f32");

    public static float[] Render(NamProcessor processor, float[] input, int blockSize)
    {
        var output = new float[input.Length];
        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            var n = Math.Min(blockSize, input.Length - offset);
            processor.Process(input.AsSpan(offset, n), output.AsSpan(offset, n));
        }
        return output;
    }

    public static float MaxAbsDifference(float[] a, float[] b)
    {
        var max = 0f;
        for (var i = 0; i < a.Length; i++)
            max = Math.Max(max, Math.Abs(a[i] - b[i]));
        return max;
    }
}
