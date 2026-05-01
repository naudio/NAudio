namespace NAudio.Utils
{
    /// <summary>
    /// Blittable version of Windows BOOLEAN type. It is convenient in situations where
    /// manual marshalling is required, or to avoid overhead of regular bool marshalling.
    /// </summary>
    /// <remarks>
    /// Some Windows APIs return arbitrary integer values although the return type is defined
    /// as BOOLEAN. It is best to never compare BOOLEAN to TRUE. Always use bResult != BOOLEAN.FALSE
    /// or bResult == BOOLEAN.FALSE .
    /// </remarks>
    public enum BOOLEAN : byte
    {
        /// <summary>
        /// Gets the value that represents the <see langword="false"/> value.
        /// </summary>
        FALSE = 0,
        /// <summary>
        /// Gets the value that represents the <see langword="true"/> value.
        /// </summary>
        TRUE = 1,
    }
}
