using System;
using System.Buffers.Binary;

namespace NAudio.Utils;

/// <summary>
/// Methods for converting between IEEE 80-bit extended double precision
/// and standard C# double precision.
/// </summary>
public static class IEEE
{
    #region Helper Methods
    private static double UnsignedToFloat(ulong u)
    {
        return u;
    }

    private static double Ldexp(double x, int exp)
    {
        return Math.ScaleB(x, exp);
    }

    private static double Frexp(double x, out int exp)
    {
        if (x == 0.0 || double.IsNaN(x) || double.IsInfinity(x))
        {
            exp = 0;
            return x;
        }
        exp = Math.ILogB(x) + 1;
        return Math.ScaleB(x, -exp);
    }

    private static ulong FloatToUnsigned(double f)
    {
        return (ulong)f;
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
        int sign = 0;
        if (num < 0)
        {
            sign = 0x8000;
            num = -num;
        }

        int expon;
        ulong hiMant, loMant;
        if (num == 0)
        {
            expon = 0;
            loMant = hiMant = 0;
        }
        else
        {
            double fMant = Frexp(num, out expon);
            if ((expon > 16384) || !(fMant < 1)) // Infinity or NaN
            {
                expon = sign | 0x7FFF; // Infinity 
                loMant = hiMant = 0;
            }
            else // Finite
            {
                expon += 16382;
                if (expon < 0) // Denormalized
                {
                    fMant = Ldexp(fMant, expon);
                    expon = 0;
                }
                expon |= sign;
                fMant = Ldexp(fMant, 32);
                double fsMant = Math.Floor(fMant);
                hiMant = FloatToUnsigned(fsMant);
                fMant = Ldexp(fMant - fsMant, 32);
                fsMant = Math.Floor(fMant);
                loMant = FloatToUnsigned(fsMant);
            }
        }

        return [
            (byte)(expon >> 8),
            (byte)(expon),
            (byte)(hiMant >> 24),
            (byte)(hiMant >> 16),
            (byte)(hiMant >> 8),
            (byte)(hiMant),
            (byte)(loMant >> 24),
            (byte)(loMant >> 16),
            (byte)(loMant >> 8),
            (byte)(loMant),
        ];
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
        ArgumentNullException.ThrowIfNull(bytes);
        return ConvertFromIeeeExtended(bytes.AsSpan());
    }

    private static double ConvertFromIeeeExtended(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 10)
        {
            throw new ArgumentException("Incorrect length for IEEE extended.", nameof(bytes));
        }

        int expon = ((bytes[0] & 0x7F) << 8) | bytes[1];
        uint hiMant = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(2, 4));
        uint loMant = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(6, 4));

        double result;
        if (expon == 0 && hiMant == 0 && loMant == 0)
        {
            result = 0;
        }
        else if (expon == 0x7FFF) // Infinity or NaN
        {
            result = double.NaN;
        }
        else
        {
            expon -= 16383;
            result = Ldexp(UnsignedToFloat(hiMant), expon -= 31);
            result += Ldexp(UnsignedToFloat(loMant), expon -= 32);
        }

        return (bytes[0] & 0x80) != 0 ? -result : result;
    }
    #endregion
}
