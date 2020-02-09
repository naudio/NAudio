using System;


namespace NAudio.Utils
{
    /// <summary>
    /// Methods for converting between IEEE 80-bit extended double precision
    /// and standard C# double precision.
    /// </summary>
    public static class IEEE
    {
        #region Helper Methods
        private static double UnsignedToFloat(ulong u)
        {
            return (((double)((long)(u - 2147483647L - 1))) + 2147483648.0);
        }

        private static double ldexp(double x, int exp)
        {
            return x * Math.Pow(2, exp);
        }

        private static double frexp(double x, out int exp)
        {
            exp = (int)Math.Floor(Math.Log(x) / Math.Log(2)) + 1;
            return 1 - (Math.Pow(2, exp) - x) / Math.Pow(2, exp);
        }

        private static ulong FloatToUnsigned(double f)
        {
            return ((ulong)(((long)(f - 2147483648.0)) + 2147483647L) + 1);
        }
        #endregion

        #region ConvertToIeeeExtended
        /// <summary>
        /// Converts a C# double precision number to an 80-bit
        /// IEEE extended double precision number (occupying 10 bytes).
        /// </summary>
        /// <param name="num">The double precision number to convert to IEEE extended.</param>
        /// <returns>An array of 10 bytes containing the IEEE extended number.</returns>
        public static byte[] ConvertToIeeeExtended(double num)
        {
            int sign;
            int expon;
            double fMant, fsMant;
            ulong hiMant, loMant;

            if (num < 0)
            {
                sign = 0x8000;
                num *= -1;
            }
            else
            {
                sign = 0;
            }

            if (num == 0)
            {
                expon = 0; hiMant = 0; loMant = 0;
            }
            else
            {
                fMant = frexp(num, out expon);
                if ((expon > 16384) || !(fMant < 1))
                {   //  Infinity or NaN 
                    expon = sign | 0x7FFF; hiMant = 0; loMant = 0; // infinity 
                }
                else
                {    // Finite 
                    expon += 16382;
                    if (expon < 0)
                    {    // denormalized
                        fMant = ldexp(fMant, expon);
                        expon = 0;
                    }
                    expon |= sign;
                    fMant = ldexp(fMant, 32);
                    fsMant = Math.Floor(fMant);
                    hiMant = FloatToUnsigned(fsMant);
                    fMant = ldexp(fMant - fsMant, 32);
                    fsMant = Math.Floor(fMant);
                    loMant = FloatToUnsigned(fsMant);
                }
            }

            byte[] bytes = new byte[10];

            bytes[0] = (byte)(expon >> 8);
            bytes[1] = (byte)(expon);
            bytes[2] = (byte)(hiMant >> 24);
            bytes[3] = (byte)(hiMant >> 16);
            bytes[4] = (byte)(hiMant >> 8);
            bytes[5] = (byte)(hiMant);
            bytes[6] = (byte)(loMant >> 24);
            bytes[7] = (byte)(loMant >> 16);
            bytes[8] = (byte)(loMant >> 8);
            bytes[9] = (byte)(loMant);

            return bytes;
        }
        #endregion

        #region ConvertFromIeeeExtended
        /// <summary>
        /// Converts an IEEE 80-bit extended precision number to a
        /// C# double precision number.
        /// </summary>
        /// <param name="bytes">The 80-bit IEEE extended number (as an array of 10 bytes).</param>
        /// <returns>A C# double precision number that is a close representation of the IEEE extended number.</returns>
        public static double ConvertFromIeeeExtended(byte[] bytes)
        {
            if (bytes.Length != 10) throw new Exception("Incorrect length for IEEE extended.");
            double f;
            int expon;
            uint hiMant, loMant;

            expon = ((bytes[0] & 0x7F) << 8) | bytes[1];
            hiMant = (uint)((bytes[2] << 24) | (bytes[3] << 16) | (bytes[4] << 8) | bytes[5]);
            loMant = (uint)((bytes[6] << 24) | (bytes[7] << 16) | (bytes[8] << 8) | bytes[9]);

            if (expon == 0 && hiMant == 0 && loMant == 0)
            {
                f = 0;
            }
            else
            {
                if (expon == 0x7FFF)    /* Infinity or NaN */
                {
                    f = double.NaN;
                }
                else
                {
                    expon -= 16383;
                    f = ldexp(UnsignedToFloat(hiMant), expon -= 31);
                    f += ldexp(UnsignedToFloat(loMant), expon -= 32);
                }
            }

            if ((bytes[0] & 0x80) == 0x80) return -f;
            else return f;
        }
        #endregion
    }
}
