namespace NAudio.Sequencing;

/// <summary>The no-op position transform. Use this when no swing/quantize/humanize is needed.</summary>
public sealed class IdentityPositionTransform : IPositionTransform
{
    /// <summary>Shared singleton instance.</summary>
    public static IdentityPositionTransform Instance { get; } = new();

    private IdentityPositionTransform() { }

    /// <inheritdoc/>
    public long Transform(long nominalTick) => nominalTick;

    /// <inheritdoc/>
    public long MaxShiftTicks => 0;
}
