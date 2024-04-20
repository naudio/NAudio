namespace NAudio.Sdl2.Structures
{
    public class WaveInSdlCapabilities
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

        public override string ToString()
        {
            return $"DeviceName = {DeviceName}; Bits = {Bits}; Channels = {Channels}; Format = {Format}; Frequency = {Frequency}; Samples = {Samples}; Silence = {Silence}; Size = {Size}";
        }
    }
}
