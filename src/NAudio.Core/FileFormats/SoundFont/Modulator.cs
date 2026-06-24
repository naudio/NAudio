using System;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Transform Types
    /// </summary>
    public enum TransformEnum
    {
        /// <summary>
        /// Linear: the multiplier output is fed directly to the destination
        /// summing node (SoundFont 2.04 §8.3).
        /// </summary>
        Linear = 0,
        /// <summary>
        /// Absolute value: the destination receives the absolute value of the
        /// multiplier output (SoundFont 2.04 §8.3).
        /// </summary>
        AbsoluteValue = 2
    }

    /// <summary>
    /// Modulator
    /// </summary>
    public class Modulator
    {

        /// <summary>
        /// Source Modulation data type
        /// </summary>
        public ModulatorType SourceModulationData { get; set; }

        /// <summary>
        /// Destination generator type
        /// </summary>
        public GeneratorEnum DestinationGenerator { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public short Amount { get; set; }

        /// <summary>
        /// Source Modulation Amount Type
        /// </summary>
        public ModulatorType SourceModulationAmount { get; set; }

        /// <summary>
        /// Source Transform Type
        /// </summary>
        public TransformEnum SourceTransform { get; set; }

        /// <summary>
        /// Whether this modulator has the same source, amount-source, destination
        /// and transform as another — i.e. they are "identical" for the purposes
        /// of the SoundFont 2.04 §9.5 combination rules, where a modulator at a
        /// more specific level supersedes one with the same identity at a more
        /// general level. The <see cref="Amount"/> is deliberately excluded.
        /// </summary>
        public bool HasIdenticalRouting(Modulator other)
        {
            return other != null
                && DestinationGenerator == other.DestinationGenerator
                && SourceTransform == other.SourceTransform
                && SourceModulationData.RawValue == other.SourceModulationData.RawValue
                && SourceModulationAmount.RawValue == other.SourceModulationAmount.RawValue;
        }

        /// <summary>
        /// <see cref="Object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return String.Format("Modulator {0} {1} {2} {3} {4}",
                SourceModulationData, DestinationGenerator,
                Amount, SourceModulationAmount, SourceTransform);
        }

    }
}