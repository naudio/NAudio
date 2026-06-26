using System.Numerics;
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
    /// Bit-depth + encoding combos to probe. 32-bit PCM and 32-bit float are distinct
    /// formats, so both are listed and <see cref="CreateFormat"/> picks the SubFormat.
    /// </summary>
    public static readonly (int bits, string encoding)[] BitDepthEncodings =
    [
        (16, "PCM"), (24, "PCM"), (32, "PCM"), (32, "Float"),
    ];

    public static readonly (int mask, string name)[] ChannelMasks =
    [
        (0, "(default)"),
        ((int)Speakers.Mono, "1.0 Mono (FC)"),
        ((int)Speakers.Stereo, "2.0 Stereo (FL|FR)"),
        ((int)(Speakers.FrontCenter | Speakers.LowFrequency), "1.1 (FC|LFE)"),
        ((int)(Speakers.Stereo | Speakers.LowFrequency), "2.1 (FL|FR|LFE)"),
        ((int)Speakers.Quad, "4.0 Quad (FL|FR|BL|BR)"),
        ((int)(Speakers.Stereo | Speakers.FrontCenter | Speakers.BackCenter), "4.0 Surround (FL|FR|FC|BC)"),
        ((int)(Speakers.Stereo | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight), "5.0 (FL|FR|FC|SL|SR)"),
        ((int)(Speakers.Quad | Speakers.FrontCenter | Speakers.LowFrequency), "5.1 Back (FL|FR|FC|LFE|BL|BR)"),
        ((int)Speakers.Surround51, "5.1 Surround (FL|FR|FC|LFE|SL|SR)"),
        ((int)(Speakers.Quad | Speakers.FrontCenter | Speakers.SideLeft | Speakers.SideRight), "7.0 (FL|FR|FC|BL|BR|SL|SR)"),
        ((int)(Speakers.Quad | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.FrontLeftOfCenter | Speakers.FrontRightOfCenter), "7.1 Wide (FL|FR|FC|LFE|BL|BR|FLC|FRC)"),
        ((int)Speakers.Surround71, "7.1 Surround (FL|FR|FC|LFE|BL|BR|SL|SR)"),
    ];

    /// <summary>
    /// Builds a <see cref="WaveFormatExtensible"/> with the requested encoding, choosing the
    /// PCM or IEEE-float SubFormat explicitly so 32-bit PCM and 32-bit float are both reachable.
    /// </summary>
    public static WaveFormatExtensible CreateFormat(int rate, int bits, int channels, string encoding, int channelMask = 0)
    {
        var subFormat = encoding == "Float"
            ? AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT
            : AudioMediaSubtypes.MEDIASUBTYPE_PCM;
        return new WaveFormatExtensible(rate, bits, channels, subFormat, bits, channelMask);
    }

    public static List<(int mask, string name)> GetMasksForChannelCount(int channelCount)
    {
        var masks = new List<(int, string)> { (0, "(default)") };
        foreach (var (mask, name) in ChannelMasks)
        {
            if (mask == 0) continue;
            if (BitOperations.PopCount((uint)mask) == channelCount) masks.Add((mask, name));
        }
        return masks;
    }
}
