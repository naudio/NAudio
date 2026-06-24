using System;

namespace NAudio.Utils
{
    /// <summary>
    /// A util class for conversions
    /// </summary>
    public class Decibels
    {
        // 20 / ln( 10 )
        private const double LOG_2_DB = 8.6858896380650365530225783783321;

        // ln( 10 ) / 20
        private const double DB_2_LOG = 0.11512925464970228420089957273422;

        /// <summary>
        /// linear to dB conversion
        /// </summary>
        /// <param name="lin">linear value</param>
        /// <returns>decibel value</returns>
        public static double LinearToDecibels(double lin)
        {
            return Math.Log(lin) * LOG_2_DB;
        }

        /// <summary>
        /// dB to linear conversion
        /// </summary>
        /// <param name="dB">decibel value</param>
        /// <returns>linear value</returns>
        public static double DecibelsToLinear(double dB)
        {
            return Math.Exp(dB * DB_2_LOG);
        }

    }
}
