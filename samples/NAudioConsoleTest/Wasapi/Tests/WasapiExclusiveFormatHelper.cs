using System.Reflection;
using NAudio.Dmo;
using NAudio.Wave;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Shared format/mask tables for the WASAPI exclusive-mode tests. Each sub-test (QuickScan,
/// DetailedScan, ChannelMaskDeepDive, FindBest) used to live as a switch case inside
/// <c>ExclusiveFormatExplorer</c>; the per-mode parameter shapes are different enough that the
/// IConsoleTest split worked out cleaner than carrying a <c>subMode</c> parameter.
/// </summary>
internal static class WasapiExclusiveFormatHelper
{
    public static readonly int[] SampleRates = [44100, 48000, 88200, 96000, 176400, 192000];
    public static readonly int[] ChannelCounts = [1, 2, 4, 6, 8];

    /// <summary>
    /// Bit-depth + encoding combos to probe. <see cref="WaveFormatExtensible"/>'s constructor
    /// pins 32-bit to IEEE float, so 32-bit PCM has to be patched separately.
    /// </summary>
    public static readonly (int bits, string encoding)[] BitDepthEncodings =
    [
        (16, "PCM"), (24, "PCM"), (32, "PCM"), (32, "Float"),
    ];

    public static readonly (int mask, string name)[] ChannelMasks =
    [
        (0, "(default)"),
        (0x0004, "1.0 Mono (FC)"),
        (0x0003, "2.0 Stereo (FL|FR)"),
        (0x000C, "1.1 (FC|LFE)"),
        (0x000B, "2.1 (FL|FR|LFE)"),
        (0x0033, "4.0 Quad (FL|FR|BL|BR)"),
        (0x0107, "4.0 Surround (FL|FR|FC|BC)"),
        (0x0607, "5.0 (FL|FR|FC|SL|SR)"),
        (0x003F, "5.1 Back (FL|FR|FC|LFE|BL|BR)"),
        (0x060F, "5.1 Surround (FL|FR|FC|LFE|SL|SR)"),
        (0x0637, "7.0 (FL|FR|FC|BL|BR|SL|SR)"),
        (0x00FF, "7.1 Wide (FL|FR|FC|LFE|BL|BR|FLC|FRC)"),
        (0x063F, "7.1 Surround (FL|FR|FC|LFE|BL|BR|SL|SR)"),
    ];

    /// <summary>
    /// Builds a <see cref="WaveFormatExtensible"/> with the requested encoding. The standard
    /// constructor hardcodes 32-bit to IEEE float — we patch <c>SubFormat</c> by reflection
    /// when 32-bit PCM is requested.
    /// </summary>
    public static WaveFormatExtensible CreateFormat(int rate, int bits, int channels, string encoding, int channelMask = 0)
    {
        var format = new WaveFormatExtensible(rate, bits, channels, channelMask);
        if (bits == 32 && encoding == "PCM")
        {
            var subFormatField = typeof(WaveFormatExtensible)
                .GetField("subFormat", BindingFlags.NonPublic | BindingFlags.Instance)!;
            subFormatField.SetValue(format, AudioMediaSubtypes.MEDIASUBTYPE_PCM);
        }
        return format;
    }

    public static List<(int mask, string name)> GetMasksForChannelCount(int channelCount)
    {
        var masks = new List<(int, string)> { (0, "(default)") };
        foreach (var (mask, name) in ChannelMasks)
        {
            if (mask == 0) continue;
            if (BitCount(mask) == channelCount) masks.Add((mask, name));
        }
        return masks;
    }

    public static int BitCount(int value)
    {
        var count = 0;
        while (value != 0) { count += value & 1; value >>= 1; }
        return count;
    }
}
