using System;

namespace NAudio.Sequencing
{
    /// <summary>
    /// A snap-based swing transform: events that sit exactly on an odd grid line are delayed by
    /// a fraction of the grid step. Events not aligned to the grid are passed through unchanged
    /// (this matches how musicians think of swing applied to a quantised drum pattern).
    /// </summary>
    public sealed class SwingTransform : IPositionTransform
    {
        private double amount;
        private long gridTicks;

        /// <summary>Creates a swing transform with the given grid (typically a 16th note) and amount.</summary>
        public SwingTransform(long gridTicks, double amount = 0.0)
        {
            GridTicks = gridTicks;
            Amount = amount;
        }

        /// <summary>
        /// The fraction of the grid step by which odd grid events are delayed. 0 = straight; 0.5 =
        /// halfway to the next grid line (a 32nd offset if the grid is 16ths); ~0.333 ≈ triplet feel.
        /// Negative values are clamped to 0 — leading swing is not supported in v1.
        /// </summary>
        public double Amount
        {
            get => amount;
            set
            {
                if (value < 0) value = 0;
                if (value > 0.95) value = 0.95;
                amount = value;
            }
        }

        /// <summary>The grid resolution in canonical ticks (typically <see cref="MusicalTime.TicksPerDivision(int)"/> with division=16).</summary>
        public long GridTicks
        {
            get => gridTicks;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Grid must be positive.");
                gridTicks = value;
            }
        }

        /// <inheritdoc/>
        public long Transform(long nominalTick)
        {
            var rem = nominalTick % gridTicks;
            if (rem != 0) return nominalTick;
            var gridPos = nominalTick / gridTicks;
            if ((gridPos & 1L) == 0) return nominalTick;
            return nominalTick + (long)(amount * gridTicks);
        }

        /// <inheritdoc/>
        public long MaxShiftTicks => (long)(amount * gridTicks) + 1;
    }
}
