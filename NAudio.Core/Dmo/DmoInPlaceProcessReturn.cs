namespace NAudio.Dmo
{
    /// <summary>
    /// Return value when Process is executed with IMediaObjectInPlace
    /// </summary>
    public enum DmoInPlaceProcessReturn
    {
        /// <summary>
        /// Success. There is no remaining data to process.
        /// </summary>
        Normal = 0x0,
        /// <summary>
        /// Success. There is still data to process.
        /// </summary>
        HasEffectTail = 0x1,
    }
}