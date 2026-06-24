using System;
using System.IO;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Represents a SoundFont
    /// </summary>
    public class SoundFont
    {
        private InfoChunk info;
        private PresetsChunk presetsChunk;
        private SampleDataChunk sampleData;

        /// <summary>
        /// Loads a SoundFont from a file
        /// </summary>
        /// <param name="fileName">Filename of the SoundFont</param>
        public SoundFont(string fileName) :
            this(new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
        }

        /// <summary>
        /// Loads a SoundFont from a stream
        /// </summary>
        /// <param name="sfFile">stream</param>
        public SoundFont(Stream sfFile)
        {
            using (sfFile) // a bit ugly, done to get Win store to compile
            {
                RiffChunk riff = RiffChunk.GetTopLevelChunk(new BinaryReader(sfFile));
                if (riff.ChunkID == "RIFF")
                {
                    string formHeader = riff.ReadChunkID();
                    if (formHeader != "sfbk")
                    {
                        throw new InvalidDataException($"Not a SoundFont ({formHeader})");
                    }
                    RiffChunk list = riff.GetNextSubChunk();
                    if (list.ChunkID == "LIST")
                    {
                        //RiffChunk r = list.GetNextSubChunk();
                        info = new InfoChunk(list);

                        RiffChunk r = riff.GetNextSubChunk();
                        sampleData = new SampleDataChunk(r);

                        r = riff.GetNextSubChunk();
                        presetsChunk = new PresetsChunk(r);
                    }
                    else
                    {
                        throw new InvalidDataException($"No info list found ({list.ChunkID})");
                    }
                }
                else
                {
                    throw new InvalidDataException("Not a RIFF file");
                }
            }
        }

        /// <summary>
        /// The File Info Chunk
        /// </summary>
        public InfoChunk FileInfo => info;

        /// <summary>
        /// The Presets
        /// </summary>
        public Preset[] Presets => presetsChunk.Presets;

        /// <summary>
        /// The Instruments
        /// </summary>
        public Instrument[] Instruments => presetsChunk.Instruments;

        /// <summary>
        /// The Sample Headers
        /// </summary>
        public SampleHeader[] SampleHeaders => presetsChunk.SampleHeaders;

        /// <summary>
        /// The raw Sample Data exactly as stored in the file's <c>smpl</c> chunk:
        /// the high 16 bits of each sample, little-endian. For 24-bit SoundFonts
        /// the least-significant 8 bits are stored separately in
        /// <see cref="SampleData24"/> (the file keeps them in a parallel
        /// <c>sm24</c> chunk so 16-bit players can ignore them). To get decoded
        /// samples at the font's full precision use <see cref="ReadSampleDataFloat"/>.
        /// </summary>
        public byte[] SampleData => sampleData.SampleData;

        /// <summary>
        /// The raw optional 24-bit extension data (the file's <c>sm24</c> chunk:
        /// one byte per sample, the low 8 bits of each sample), or null if the
        /// SoundFont contains only 16-bit samples. Pairs one byte with each
        /// 16-bit sample in <see cref="SampleData"/>; combine as
        /// <c>(smpl16 &lt;&lt; 8) | sm24</c>, or use <see cref="ReadSampleDataFloat"/>.
        /// </summary>
        public byte[] SampleData24 => sampleData.SampleData24;

        /// <summary>
        /// Whether this SoundFont carries 24-bit sample data (an sm24 sub-chunk).
        /// </summary>
        public bool Has24BitSamples => sampleData.SampleData24 != null;

        /// <summary>
        /// Decodes the sample pool to normalised 32-bit float at the font's full
        /// available precision: 16-bit samples from <see cref="SampleData"/>,
        /// extended with the low bytes from <see cref="SampleData24"/> when the
        /// font is 24-bit (each sample is <c>(smpl16 &lt;&lt; 8) | sm24</c>,
        /// scaled by 2^23). <see cref="SampleHeader"/> start/end/loop addresses
        /// index the result directly (one float per sample point). Allocates a
        /// new array on every call.
        /// </summary>
        public float[] ReadSampleDataFloat()
        {
            byte[] data = sampleData.SampleData;
            byte[] low = sampleData.SampleData24;
            int count = data.Length / 2;
            var samples = new float[count];

            if (low != null && low.Length >= count)
            {
                const float scale = 1f / 8388608f; // 2^23
                for (int i = 0; i < count; i++)
                {
                    short high = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                    int value = (high << 8) | low[i];
                    samples[i] = value * scale;
                }
            }
            else
            {
                const float scale = 1f / 32768f;
                for (int i = 0; i < count; i++)
                {
                    short value = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                    samples[i] = value * scale;
                }
            }
            return samples;
        }

        /// <summary>
        /// <see cref="Object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            return $"Info Chunk:\r\n{info}\r\nPresets Chunk:\r\n{presetsChunk}";
        }

        // TODO: save / save as function
    }
}
