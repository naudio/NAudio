namespace NAudio.Sequencing;

/// <summary>
/// Transforms a nominal event tick into an effective tick. Used to layer swing / quantize /
/// humanize on top of a stored event timeline without rewriting the events. The transform must
/// declare a tight bound on the magnitude of any shift it can apply, so the consumer can
/// over-scan its timeline query (see the architecture doc for why this is essential).
/// </summary>
public interface IPositionTransform
{
    /// <summary>The effective tick for an event whose stored (nominal) tick is <paramref name="nominalTick"/>.</summary>
    long Transform(long nominalTick);

    /// <summary>
    /// An upper bound on |effective − nominal| over all inputs. Consumers use this to expand the
    /// nominal range they query so no event is silently dropped by the transform pushing it across
    /// a buffer boundary.
    /// </summary>
    long MaxShiftTicks { get; }
}
