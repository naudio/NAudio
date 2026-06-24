namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// The PartType enumeration defines constants that indicate whether a part in a device topology is a connector or subunit.
    /// </summary>
    public enum PartTypeEnum
    {
        /// <summary>
        /// Connector
        /// </summary>
        Connector = 0,
        /// <summary>
        /// Subunit
        /// </summary>
        Subunit = 1,
        /// <summary>
        /// Hardware Periphery
        /// </summary>
        HardwarePeriphery = 2,
        /// <summary>
        /// Software Driver
        /// </summary>
        SoftwareDriver = 3,
        /// <summary>
        /// Splitter
        /// </summary>
        Splitter = 4,
        /// <summary>
        /// Category
        /// </summary>
        Category = 5,
        /// <summary>
        /// Other
        /// </summary>
        Other = 6
    }
}
