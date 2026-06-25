using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NAudio.Sampler;

/// <summary>
/// A lookup index over one region list, so the engine's hot dispatch paths
/// don't linearly scan every region: 128 per-key buckets of note-on
/// candidates, plus the release-triggered and CC-triggered sublists with
/// O(1) empty checks (for SF2 instruments these are always empty, yet
/// note-off and every CC change used to scan the whole list to find that
/// out). Bucket and sublist contents preserve the original list order, so
/// layer dispatch, round-robin advancement and voice-steal ordering are
/// unchanged.
///
/// Indexes are cached by region-list identity: the samplers cache their
/// lists (rebuilding them only when the instrument changes), so identity is
/// a correct cache key, and the weak table lets a replaced list's index be
/// collected.
/// </summary>
internal sealed class RegionIndex
{
    private static readonly ConditionalWeakTable<IReadOnlyList<SamplerRegion>, RegionIndex> cache = new();
    private static readonly ConditionalWeakTable<IReadOnlyList<SamplerRegion>, RegionIndex>.CreateValueCallback factory =
        regions => new RegionIndex(regions);

    private static readonly SamplerRegion[] Empty = Array.Empty<SamplerRegion>();

    private readonly SamplerRegion[][] noteOnByKey;

    /// <summary>Regions with <see cref="SamplerTrigger.Release"/>, in list order.</summary>
    public SamplerRegion[] ReleaseTriggered { get; }

    /// <summary>Regions triggered by a CC window rather than a key, in list order.</summary>
    public SamplerRegion[] CcTriggered { get; }

    /// <summary>The (cached) index for a region list.</summary>
    public static RegionIndex For(IReadOnlyList<SamplerRegion> regions) => cache.GetValue(regions, factory);

    /// <summary>
    /// The non-CC-triggered regions whose key range covers <paramref name="key"/>,
    /// in original list order. The engine still applies the velocity/trigger/
    /// gate checks per note-on.
    /// </summary>
    public SamplerRegion[] NoteOnCandidates(int key) =>
        (uint)key < (uint)noteOnByKey.Length ? noteOnByKey[key] : Empty;

    private RegionIndex(IReadOnlyList<SamplerRegion> regions)
    {
        var buckets = new List<SamplerRegion>[128];
        List<SamplerRegion> releases = null;
        List<SamplerRegion> ccTriggered = null;

        for (int i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            if (region == null) continue;

            if (region.Trigger == SamplerTrigger.Release)
                (releases ??= new List<SamplerRegion>()).Add(region);
            if (region.IsCcTriggered)
            {
                (ccTriggered ??= new List<SamplerRegion>()).Add(region);
                continue; // never a note-on candidate
            }

            int hi = Math.Min(region.HiKey, (byte)127);
            for (int key = region.LoKey; key <= hi; key++)
                (buckets[key] ??= new List<SamplerRegion>()).Add(region);
        }

        noteOnByKey = new SamplerRegion[128][];
        for (int key = 0; key < 128; key++)
            noteOnByKey[key] = buckets[key] == null ? Empty : buckets[key].ToArray();
        ReleaseTriggered = releases == null ? Empty : releases.ToArray();
        CcTriggered = ccTriggered == null ? Empty : ccTriggered.ToArray();
    }
}
