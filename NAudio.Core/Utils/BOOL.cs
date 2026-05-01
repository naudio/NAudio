namespace NAudio.Utils
{
    /// <summary>
    /// Blittable version of Windows BOOL type. It is convenient in situations where
    /// manual marshalling is required, or to avoid overhead of regular bool marshalling.
    /// </summary>
    /// <remarks>
    /// Some Windows APIs return arbitrary integer values although the return type is defined
    /// as BOOL. It is best to never compare BOOL to TRUE. Always use bResult != BOOL.FALSE
    /// or bResult == BOOL.FALSE .
    /// </remarks>
    public enum BOOL : int
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
