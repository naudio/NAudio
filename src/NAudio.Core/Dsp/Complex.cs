using System.Runtime.CompilerServices;

namespace NAudio.Dsp
{
    /// <summary>
    /// Type to represent complex number
    /// </summary>
    public struct Complex
    {
        /// <summary>
        /// Real Part. Equivalent to <see cref="Real"/>; retained for backward compatibility.
        /// </summary>
        public float X;
        /// <summary>
        /// Imaginary Part. Equivalent to <see cref="Imaginary"/>; retained for backward compatibility.
        /// </summary>
        public float Y;

        /// <summary>
        /// Real part of the complex number.
        /// </summary>
        public float Real
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => X;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => X = value;
        }

        /// <summary>
        /// Imaginary part of the complex number.
        /// </summary>
        public float Imaginary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Y;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Y = value;
        }
    }
}
