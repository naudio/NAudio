using System;

namespace NAudio.Core
{
    public static class Extensions
    {
        private const double Epsilon = 0.001;

        /// <summary>
        /// Returns true if the floating-point values are equal within the specified margin of error.
        /// </summary>
        /// <param name="val1">First value.</param>
        /// <param name="val2">Second value.</param>
        public static bool AreEqual(this double val1, double val2)
        {
            return Math.Abs(val1 - val2) < Epsilon;
        }

        /// <summary>
        /// Returns true if the floating-point values are equal within the specified margin of error.
        /// </summary>
        /// <param name="val1">First value.</param>
        /// <param name="val2">Second value.</param>
        public static bool AreEqual(this float val1, float val2)
        {
            return Math.Abs(val1 - val2) < Epsilon;
        }
    }
}
