using System.Collections.Generic;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Flattens a SoundFont <see cref="Preset"/> into a list of fully-resolved,
    /// playable <see cref="SoundFontRegion"/>s, applying the SoundFont 2.04
    /// generator model (§9.4):
    /// <list type="bullet">
    /// <item>instrument-zone generators are <em>absolute</em> values applied on
    /// top of the SoundFont defaults;</item>
    /// <item>a global instrument zone (the first zone with no sample) supplies
    /// defaults for the other instrument zones;</item>
    /// <item>preset-zone generators are <em>additive offsets</em> added to the
    /// instrument result, with a global preset zone supplying preset-level
    /// defaults;</item>
    /// <item>key and velocity ranges are the intersection of the preset-zone
    /// and instrument-zone ranges.</item>
    /// </list>
    /// The result is independent of the rest of the file, so it can be cached
    /// per preset and handed to the synthesiser.
    /// </summary>
    public static class SoundFontInstrumentResolver
    {
        /// <summary>
        /// Resolves all playable regions of a preset.
        /// </summary>
        public static IReadOnlyList<SoundFontRegion> Resolve(Preset preset)
        {
            var regions = new List<SoundFontRegion>();
            if (preset?.Zones == null) return regions;

            // A global preset zone (no Instrument generator) supplies defaults
            // for the remaining preset zones.
            var globalPreset = TryGetGlobalZone(preset.Zones, GeneratorEnum.Instrument);

            foreach (var presetZone in preset.Zones)
            {
                var instrument = FindInstrument(presetZone);
                if (instrument?.Zones == null) continue; // global or empty zone

                // preset-level offsets: the global zone supplies defaults that a
                // local zone's generator supersedes (the offsets are then *added*
                // to the instrument level, per §9.4)
                var presetOffsets = SoundFontGenerators.CreateZeroed();
                if (globalPreset != null) ApplyPresetOffsets(globalPreset, presetOffsets);
                ApplyPresetOffsets(presetZone, presetOffsets);

                var presetKeyRange = GetRange(presetZone, GeneratorEnum.KeyRange, globalPreset);
                var presetVelRange = GetRange(presetZone, GeneratorEnum.VelocityRange, globalPreset);

                // preset-level modulators: global zone first, then this zone, so a
                // local modulator supersedes an identically-routed global one (§9.5)
                var presetModulators = CombineModulators(globalPreset, presetZone);

                ResolveInstrument(instrument, presetOffsets, presetKeyRange, presetVelRange,
                    presetModulators, regions);
            }

            return regions;
        }

        private static void ResolveInstrument(Instrument instrument,
            SoundFontGenerators presetOffsets, Range presetKeyRange, Range presetVelRange,
            IReadOnlyList<Modulator> presetModulators, List<SoundFontRegion> regions)
        {
            var globalInstrument = TryGetGlobalZone(instrument.Zones, GeneratorEnum.SampleID);

            foreach (var instrumentZone in instrument.Zones)
            {
                var sample = FindSample(instrumentZone);
                if (sample == null) continue; // global or empty zone

                // instrument-level absolute values: defaults, then global zone, then this zone
                var generators = SoundFontGenerators.CreateWithDefaults();
                if (globalInstrument != null) ApplyAbsolute(globalInstrument, generators);
                ApplyAbsolute(instrumentZone, generators);

                // add the preset-level offsets on top
                AddOffsets(presetOffsets, generators);

                var keyRange = GetRange(instrumentZone, GeneratorEnum.KeyRange, globalInstrument)
                    .Intersect(presetKeyRange);
                var velRange = GetRange(instrumentZone, GeneratorEnum.VelocityRange, globalInstrument)
                    .Intersect(presetVelRange);

                if (keyRange.IsEmpty || velRange.IsEmpty) continue;

                var instrumentModulators = CombineModulators(globalInstrument, instrumentZone);

                regions.Add(new SoundFontRegion(sample, generators,
                    (byte)keyRange.Low, (byte)keyRange.High,
                    (byte)velRange.Low, (byte)velRange.High,
                    instrumentModulators, presetModulators));
            }
        }

        /// <summary>
        /// Concatenates a global zone's modulators (if any) and a local zone's,
        /// global first, for the §9.5 combination the synthesiser performs (later
        /// entries supersede earlier ones with identical routing). Returns null
        /// when neither zone has modulators, so the region keeps its empty default.
        /// </summary>
        private static IReadOnlyList<Modulator> CombineModulators(Zone globalZone, Zone localZone)
        {
            var global = globalZone?.Modulators;
            var local = localZone?.Modulators;
            bool hasGlobal = global != null && global.Length > 0;
            bool hasLocal = local != null && local.Length > 0;
            if (!hasGlobal && !hasLocal) return null;
            if (!hasGlobal) return local;
            if (!hasLocal) return global;

            var combined = new Modulator[global.Length + local.Length];
            global.CopyTo(combined, 0);
            local.CopyTo(combined, global.Length);
            return combined;
        }

        private static Instrument FindInstrument(Zone zone)
        {
            if (zone.Generators == null) return null;
            foreach (var g in zone.Generators)
                if (g.GeneratorType == GeneratorEnum.Instrument) return g.Instrument;
            return null;
        }

        private static SampleHeader FindSample(Zone zone)
        {
            if (zone.Generators == null) return null;
            foreach (var g in zone.Generators)
                if (g.GeneratorType == GeneratorEnum.SampleID) return g.SampleHeader;
            return null;
        }

        /// <summary>
        /// A global zone is the (first) zone that lacks the terminal index
        /// generator. The spec requires it to be first, but we scan defensively.
        /// </summary>
        private static Zone TryGetGlobalZone(Zone[] zones, GeneratorEnum indexGenerator)
        {
            if (zones.Length == 0) return null;
            var first = zones[0];
            if (first.Generators == null) return first;
            foreach (var g in first.Generators)
                if (g.GeneratorType == indexGenerator) return null; // not global
            return first;
        }

        private static void ApplyAbsolute(Zone zone, SoundFontGenerators target)
        {
            if (zone.Generators == null) return;
            foreach (var g in zone.Generators)
            {
                if (IsRangeOrIndex(g.GeneratorType)) continue;
                target[g.GeneratorType] = g.Int16Amount;
            }
        }

        private static void ApplyPresetOffsets(Zone zone, SoundFontGenerators target)
        {
            if (zone.Generators == null) return;
            foreach (var g in zone.Generators)
            {
                if (!IsAllowedAtPresetLevel(g.GeneratorType)) continue;
                // assignment, not addition: a generator in the local preset zone
                // supersedes the global preset zone's value (§9.4) — the *additive*
                // step is applying these offsets to the instrument level, which
                // happens once in AddOffsets
                target[g.GeneratorType] = g.Int16Amount;
            }
        }

        private static void AddOffsets(SoundFontGenerators offsets, SoundFontGenerators target)
        {
            for (int i = 0; i <= (int)GeneratorEnum.UnusedEnd; i++)
            {
                var gen = (GeneratorEnum)i;
                if (!IsAllowedAtPresetLevel(gen)) continue;
                target[gen] += offsets[gen];
            }
        }

        private static Range GetRange(Zone zone, GeneratorEnum rangeGenerator, Zone globalZone)
        {
            // an explicit range on the zone wins; otherwise inherit the global
            // zone's range; otherwise the full 0-127 range
            if (TryGetRange(zone, rangeGenerator, out var range)) return range;
            if (globalZone != null && TryGetRange(globalZone, rangeGenerator, out range)) return range;
            return new Range(0, 127);
        }

        private static bool TryGetRange(Zone zone, GeneratorEnum rangeGenerator, out Range range)
        {
            if (zone.Generators != null)
            {
                foreach (var g in zone.Generators)
                {
                    if (g.GeneratorType == rangeGenerator)
                    {
                        range = new Range(g.LowByteAmount, g.HighByteAmount);
                        return true;
                    }
                }
            }
            range = default;
            return false;
        }

        private static bool IsRangeOrIndex(GeneratorEnum g) =>
            g == GeneratorEnum.KeyRange || g == GeneratorEnum.VelocityRange ||
            g == GeneratorEnum.Instrument || g == GeneratorEnum.SampleID;

        /// <summary>
        /// Whether a generator may appear as a preset-level additive offset.
        /// The sample-address offsets, keynum/velocity overrides, sampleModes,
        /// exclusiveClass and overridingRootKey are instrument-only (SF2.04
        /// §8.1.2) and are ignored if present in a preset zone; range and index
        /// generators are handled separately.
        /// </summary>
        private static bool IsAllowedAtPresetLevel(GeneratorEnum g)
        {
            switch (g)
            {
                case GeneratorEnum.StartAddressOffset:
                case GeneratorEnum.EndAddressOffset:
                case GeneratorEnum.StartLoopAddressOffset:
                case GeneratorEnum.EndLoopAddressOffset:
                case GeneratorEnum.StartAddressCoarseOffset:
                case GeneratorEnum.EndAddressCoarseOffset:
                case GeneratorEnum.StartLoopAddressCoarseOffset:
                case GeneratorEnum.EndLoopAddressCoarseOffset:
                case GeneratorEnum.KeyNumber:
                case GeneratorEnum.Velocity:
                case GeneratorEnum.SampleModes:
                case GeneratorEnum.ExclusiveClass:
                case GeneratorEnum.OverridingRootKey:
                case GeneratorEnum.KeyRange:
                case GeneratorEnum.VelocityRange:
                case GeneratorEnum.Instrument:
                case GeneratorEnum.SampleID:
                case GeneratorEnum.Unused1:
                case GeneratorEnum.Unused2:
                case GeneratorEnum.Unused3:
                case GeneratorEnum.Unused4:
                case GeneratorEnum.Unused5:
                case GeneratorEnum.UnusedEnd:
                case GeneratorEnum.Reserved1:
                case GeneratorEnum.Reserved2:
                case GeneratorEnum.Reserved3:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// An inclusive [Low, High] key/velocity range.
        /// </summary>
        private readonly struct Range
        {
            public Range(int low, int high)
            {
                Low = low;
                High = high;
            }

            public int Low { get; }
            public int High { get; }
            public bool IsEmpty => Low > High;

            public Range Intersect(Range other) =>
                new Range(Low > other.Low ? Low : other.Low,
                          High < other.High ? High : other.High);
        }
    }
}
