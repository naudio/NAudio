namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// The EndpointFormFactor enumeration defines constants that indicate the general physical attributes of an audio endpoint device.
    /// </summary>
    public enum EndpointFormFactor : uint
    {
        /// <summary>
        /// An audio endpoint device that the user accesses remotely through a network.
        /// </summary>
        RemoteNetworkDevice,
        /// <summary>
        /// A set of speakers.
        /// </summary>
        Speakers,
        /// <summary>
        /// An audio endpoint device that sends a line-level analog signal to a line-input jack on an audio adapter 
        /// or that receives a line-level analog signal from a line-output jack on the adapter.
        /// </summary>
        LineLevel,
        /// <summary>
        /// A set of headphones.
        /// </summary>
        Headphones,
        /// <summary>
        /// A microphone.
        /// </summary>
        Microphone,
        /// <summary>
        /// An earphone or a pair of earphones with an attached mouthpiece for two-way communication.
        /// </summary>
        Headset,
        /// <summary>
        /// The part of a telephone that is held in the hand and that contains a speaker and a microphone 
        /// for two-way communication.
        /// </summary>
        Handset,
        /// <summary>
        /// An audio endpoint device that connects to an audio adapter through a connector for a digital
        /// interface of unknown type that transmits non-PCM data in digital pass-through mode.
        /// </summary>
        UnknownDigitalPassthrough,
        /// <summary>
        /// An audio endpoint device that connects to an audio adapter through a Sony/Philips Digital
        /// Interface (S/PDIF) connector.
        /// </summary>
        SPDIF,
        /// <summary>
        /// An audio endpoint device that connects to an audio adapter through a High-Definition Multimedia
        /// Interface (HDMI) connector or a display port.       
        /// </summary>
        HDMI,
        /// <summary>
        /// An audio endpoint device with unknown physical attributes.
        /// </summary>
        UnknownFormFactor
    }
}
