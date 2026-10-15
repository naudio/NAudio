using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression;

/// <summary>
/// Summary description for WaveFilter.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
[System.Obsolete("ACM filter (same-format effect) drivers are effectively obsolete and NAudio provides no way to enumerate them. To use a codec that isn't installed, use AcmDriver.AddLocalDriver. WaveFilter will be removed in a future major version.")]
public class WaveFilter
{
    /// <summary>
    /// cbStruct
    /// </summary>
    public int StructureSize = Marshal.SizeOf<WaveFilter>();
    /// <summary>
    /// dwFilterTag
    /// </summary>
    public int FilterTag = 0;
    /// <summary>
    /// fdwFilter
    /// </summary>
    public int Filter = 0;
    /// <summary>
    /// reserved
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public int[]? Reserved = null;
}
