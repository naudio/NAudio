namespace NAudio.Sdl2.Structures
{
    public struct WaveOutSdlCapabilities
    {
        public int DeviceNumber { get; set; }
        public string DeviceName { get; set; }
        public ushort Bits { get; set; }
        public int Channels { get; set; }
        public ushort Format { get; set; }
        public int Frequency { get; set; }
        public ushort Samples { get; set; }
        public byte Silence { get; set; }
        public uint Size { get; set; }
        public bool IsAudioCapabilitiesValid { get; internal set; }

        public string ToString(string separator)
        {
            return $"Number = {DeviceNumber}" +
                   $"{separator}" +
                   $"Name = {DeviceName}" +
                   $"{separator}" +
                   $"Bits = {Bits}" +
                   $"{separator}" +
                   $"Channels = {Channels}" +
                   $"{separator}" +
                   $"Format = {Format}" +
                   $"{separator}" +
                   $"Frequency = {Frequency}" +
                   $"{separator}" +
                   $"Samples = {Samples}" +
                   $"{separator}" +
                   $"Silence = {Silence}" +
                   $"{separator}" +
                   $"Size = {Size}" +
                   $"{separator}" +
                   $"IsAudioCapabilitiesValid = {IsAudioCapabilitiesValid}";
        }
    }
}
