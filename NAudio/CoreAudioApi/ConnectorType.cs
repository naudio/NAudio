namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Connector Type
    /// </summary>
    public enum ConnectorType
    {
        /// <summary>
        /// The connector is part of a connection of unknown type.
        /// </summary>
        UnknownConnector,
        /// <summary>
        /// The connector is part of a physical connection to an auxiliary device that is installed inside the system chassis
        /// </summary>
        PhysicalInternal,
        /// <summary>
        /// The connector is part of a physical connection to an external device.
        /// </summary>
        PhysicalExternal,
        /// <summary>
        /// The connector is part of a software-configured I/O connection (typically a DMA channel) between system memory and an audio hardware device on an audio adapter.
        /// </summary>
        SoftwareIo,
        /// <summary>
        /// The connector is part of a permanent connection that is fixed and cannot be configured under software control.
        /// </summary>
        SoftwareFixed,
        /// <summary>
        /// The connector is part of a connection to a network.
        /// </summary>
        Network,
    }
}
