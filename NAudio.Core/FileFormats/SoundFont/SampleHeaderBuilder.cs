using System.IO;
using NAudio.Utils;

namespace NAudio.SoundFont
{
    class SampleHeaderBuilder : StructureBuilder<SampleHeader>
    {
        public override SampleHeader Read(BinaryReader br)
        {
            SampleHeader sh = new SampleHeader();
            var s = br.ReadBytes(20);

            sh.SampleName = ByteEncoding.Instance.GetString(s, 0, s.Length);
            sh.Start = br.ReadUInt32();
            sh.End = br.ReadUInt32();
            sh.StartLoop = br.ReadUInt32();
            sh.EndLoop = br.ReadUInt32();
            sh.SampleRate = br.ReadUInt32();
            sh.OriginalPitch = br.ReadByte();
            sh.PitchCorrection = br.ReadSByte();
            sh.SampleLink = br.ReadUInt16();
            sh.SFSampleLink = (SFSampleLink)br.ReadUInt16();
            data.Add(sh);
            return sh;
        }

        public override void Write(BinaryWriter bw, SampleHeader sampleHeader)
        {
        }

        public override int Length => 46;

        internal void RemoveEOS()
        {
            data.RemoveAt(data.Count - 1);
        }

        public SampleHeader[] SampleHeaders => data.ToArray();
    }
}