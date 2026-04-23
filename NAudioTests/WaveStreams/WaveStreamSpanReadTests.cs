using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NAudio.Utils;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// Parity and architectural tests for the Span-based Read path on WaveStream subclasses.
    /// Ensures: (a) calling Read(Span&lt;byte&gt;) yields the same bytes as Read(byte[], int, int);
    /// (b) NAudio's concrete readers all override Read(Span&lt;byte&gt;) directly (no bridge-copy).
    /// </summary>
    [TestFixture]
    public class WaveStreamSpanReadTests
    {
        /// <summary>
        /// Build a 1kHz sine-in-WAV byte array we can feed repeatedly to readers.
        /// </summary>
        private static byte[] Build16BitMonoPcmWav(int sampleCount = 4096, int sampleRate = 44100)
        {
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(sampleRate, 16, 1)))
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = (short)(Math.Sin(2 * Math.PI * 1000.0 * i / sampleRate) * 16000);
                    writer.WriteSample(sample / 32768f);
                }
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Read a bounded number of bytes from a WaveStream using the byte[] overload.
        /// Some streams (WaveOffsetStream, WaveChannel32 with PadWithZeroes) never return 0 — the
        /// caller's expected playout length terminates the loop instead.
        /// </summary>
        private static byte[] ReadAllViaByteArray(WaveStream stream, int chunkSize, long? expectedLength = null)
        {
            stream.Position = 0;
            long bound = expectedLength ?? stream.Length;
            var ms = new MemoryStream();
            var buffer = new byte[chunkSize];
            int read;
            while (ms.Length < bound && (read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, bound - ms.Length))) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Read a bounded number of bytes from a WaveStream using the Span overload.
        /// </summary>
        private static byte[] ReadAllViaSpan(WaveStream stream, int chunkSize, long? expectedLength = null)
        {
            stream.Position = 0;
            long bound = expectedLength ?? stream.Length;
            var ms = new MemoryStream();
            var buffer = new byte[chunkSize];
            int read;
            while (ms.Length < bound && (read = stream.Read(buffer.AsSpan(0, (int)Math.Min(buffer.Length, bound - ms.Length)))) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        private static void AssertReadParity(WaveStream stream, int chunkSize = 1024, long? expectedLength = null)
        {
            var viaArray = ReadAllViaByteArray(stream, chunkSize, expectedLength);
            var viaSpan = ReadAllViaSpan(stream, chunkSize, expectedLength);
            Assert.That(viaSpan, Is.EqualTo(viaArray),
                $"Span read and byte[] read produced different data on {stream.GetType().Name}");
            Assert.That(viaArray.Length, Is.GreaterThan(0),
                $"Test did not actually read anything from {stream.GetType().Name}");
        }

        [Test]
        public void WaveFileReader_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            using var reader = new WaveFileReader(new MemoryStream(wav));
            AssertReadParity(reader);
        }

        [Test]
        public void WaveFileReader_SpanReadRespectsSliceBoundary()
        {
            var wav = Build16BitMonoPcmWav();
            using var reader = new WaveFileReader(new MemoryStream(wav));
            // Allocate a larger buffer and read through a sliced span; outside the slice must stay 0xFF.
            var outer = new byte[4096];
            Array.Fill(outer, (byte)0xFF);
            int got = reader.Read(outer.AsSpan(256, 1024));
            Assert.That(got, Is.EqualTo(1024));
            Assert.That(outer[0], Is.EqualTo(0xFF), "Bytes before the slice should be untouched");
            Assert.That(outer[255], Is.EqualTo(0xFF), "Byte immediately before slice should be untouched");
            Assert.That(outer[256 + 1024], Is.EqualTo(0xFF), "Byte immediately after slice should be untouched");
        }

        [Test]
        public void RawSourceWaveStream_SpanAndByteArrayRead_Agree()
        {
            var data = new byte[4096];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)(i & 0xFF);
            var stream = new RawSourceWaveStream(new MemoryStream(data), new WaveFormat(44100, 16, 1));
            AssertReadParity(stream);
        }

        [Test]
        public void WaveOffsetStream_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            using var source = new WaveFileReader(new MemoryStream(wav));
            using var offset = new WaveOffsetStream(source, TimeSpan.FromMilliseconds(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
            AssertReadParity(offset);
        }

        [Test]
        public void BlockAlignReductionStream_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            using var source = new WaveFileReader(new MemoryStream(wav));
            using var reducer = new BlockAlignReductionStream(source);
            AssertReadParity(reducer);
        }

        [Test]
        public void WaveChannel32_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            var source = new WaveFileReader(new MemoryStream(wav));
            using var channel = new WaveChannel32(source);
            AssertReadParity(channel, chunkSize: 2048); // multiple of 8 (stereo float)
        }

        [Test]
        public void Wave32To16Stream_SpanAndByteArrayRead_Agree()
        {
            // Build a 32-bit IEEE float source, then push it through Wave32To16Stream
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms),
                       WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)))
            {
                for (int i = 0; i < 4096; i++)
                    writer.WriteSample((float)Math.Sin(2 * Math.PI * 1000.0 * i / 44100) * 0.8f);
            }
            var source = new WaveFileReader(new MemoryStream(ms.ToArray()));
            using var converter = new Wave32To16Stream(source);
            AssertReadParity(converter);
        }

        [Test]
        public void WaveMixerStream32_SpanAndByteArrayRead_Agree()
        {
            var mixer = new WaveMixerStream32 { AutoStop = true };
            for (int s = 0; s < 3; s++)
            {
                var ms = new MemoryStream();
                using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms),
                           WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)))
                {
                    for (int i = 0; i < 2048; i++)
                    {
                        writer.WriteSample(0.1f * (s + 1));
                        writer.WriteSample(0.1f * (s + 1));
                    }
                }
                mixer.AddInputStream(new WaveFileReader(new MemoryStream(ms.ToArray())));
            }
            using (mixer)
            {
                AssertReadParity(mixer, chunkSize: 2048);
            }
        }

        [Test]
        public void AudioFileReader_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            var tmp = Path.Combine(Path.GetTempPath(), "naudio-span-test-" + Guid.NewGuid() + ".wav");
            File.WriteAllBytes(tmp, wav);
            try
            {
                using var reader = new AudioFileReader(tmp);
                AssertReadParity(reader, chunkSize: 2048); // float stereo output
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        /// <summary>
        /// Build an AIFF byte array of the requested PCM bit depth. Each bit depth exercises a
        /// different endian-swap branch inside AiffFileReader.Read.
        /// </summary>
        private static byte[] BuildMonoAiff(int bitsPerSample, int sampleCount = 1024, int sampleRate = 44100)
        {
            // AiffFileWriter only accepts 32-bit PCM via WaveFormatExtensible; 16/24-bit use the
            // regular PCM WaveFormat.
            WaveFormat format = bitsPerSample == 32
                ? new WaveFormatExtensible(sampleRate, bitsPerSample, 1)
                : new WaveFormat(sampleRate, bitsPerSample, 1);
            var ms = new MemoryStream();
            using (var writer = new AiffFileWriter(new IgnoreDisposeStream(ms), format))
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    writer.WriteSample((float)Math.Sin(2 * Math.PI * 1000.0 * i / sampleRate) * 0.8f);
                }
            }
            return ms.ToArray();
        }

        [TestCase(16)]
        [TestCase(24)]
        [TestCase(32)]
        public void AiffFileReader_SpanAndByteArrayRead_Agree(int bitsPerSample)
        {
            var aiff = BuildMonoAiff(bitsPerSample);
            using var reader = new AiffFileReader(new MemoryStream(aiff));
            AssertReadParity(reader, chunkSize: bitsPerSample / 8 * 256);
        }

        /// <summary>
        /// The legacy <c>Read(byte[], int, int)</c> overload had been broken for non-zero offsets
        /// (it would throw because its internal scratch buffer was sized to count, not offset+count).
        /// After the span refactor the offset is honoured — this test pins that behavior down.
        /// </summary>
        [Test]
        public void AiffFileReader_ReadWithNonZeroOffset_FillsOnlyRequestedSlice()
        {
            var aiff = BuildMonoAiff(16);
            using var reader = new AiffFileReader(new MemoryStream(aiff));

            // Read the whole thing once via offset 0 as the reference.
            var reference = new byte[(int)reader.Length];
            Assert.That(reader.Read(reference, 0, reference.Length), Is.EqualTo(reference.Length));

            // Now read again into a larger sentinel-filled buffer at a non-zero offset.
            reader.Position = 0;
            var outer = new byte[reference.Length + 512];
            Array.Fill(outer, (byte)0xCD);
            int got = reader.Read(outer, 256, reference.Length);

            Assert.That(got, Is.EqualTo(reference.Length));
            Assert.That(outer[0], Is.EqualTo(0xCD), "Bytes before the slice should be untouched");
            Assert.That(outer[255], Is.EqualTo(0xCD), "Byte immediately before slice should be untouched");
            Assert.That(outer[256 + reference.Length], Is.EqualTo(0xCD), "Byte immediately after slice should be untouched");
            Assert.That(outer.AsSpan(256, reference.Length).ToArray(), Is.EqualTo(reference),
                "Data written at offset 256 should match the data read at offset 0");
        }

        /// <summary>
        /// Third-party subclasses that only override the legacy byte[] overload must still
        /// deliver correct data when the caller invokes Read(Span&lt;byte&gt;) via the base class bridge.
        /// </summary>
        [Test]
        public void LegacyByteArrayOnlySubclass_StillBridgesToSpanCallers()
        {
            var stream = new NullWaveStream(new WaveFormat(44100, 16, 1), 4096);

            // Fill the buffer with a sentinel so we can see that the bridge actually wrote into it.
            var buffer = new byte[1024];
            Array.Fill(buffer, (byte)0xAA);

            int read = stream.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(1024));
            // NullWaveStream writes zeros — every byte should be 0 after the read.
            Assert.That(buffer.All(b => b == 0), Is.True,
                "Legacy byte[]-only subclass should deliver its data through the Span bridge");
        }

        /// <summary>
        /// Architectural test: every concrete WaveStream subclass in NAudio's own assemblies should
        /// override Read(Span&lt;byte&gt;) directly. Relying on the Stream default bridge for our own
        /// readers means an ArrayPool rent + a copy per read — avoidable for everything we ship.
        /// </summary>
        [Test]
        public void AllNAudioWaveStreamSubclasses_OverrideSpanRead()
        {
            var assembliesToCheck = new[]
            {
                typeof(WaveFileReader).Assembly,           // NAudio.Core
                typeof(WaveFormatConversionStream).Assembly, // NAudio.WinMM
                typeof(AudioFileReader).Assembly,          // NAudio umbrella
            };

            var spanReadSig = new[] { typeof(Span<byte>) };
            var byteArrayReadSig = new[] { typeof(byte[]), typeof(int), typeof(int) };

            // Classes we know don't need to override (or physically can't — they're abstract-ish bases).
            // Keep this list tight and justified.
            var exempt = new[]
            {
                "NAudio.Wave.WaveStream", // abstract base
            };

            var offenders = new System.Collections.Generic.List<string>();
            foreach (var asm in assembliesToCheck)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract) continue;
                    if (!typeof(WaveStream).IsAssignableFrom(type)) continue;
                    if (exempt.Contains(type.FullName)) continue;

                    // Does this type (or any non-WaveStream base between it and WaveStream) override Read(Span<byte>)?
                    bool overridesSpan = false;
                    var t = type;
                    while (t != null && t != typeof(WaveStream))
                    {
                        var m = t.GetMethod("Read",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                            binder: null, types: spanReadSig, modifiers: null);
                        if (m != null) { overridesSpan = true; break; }
                        t = t.BaseType;
                    }

                    // And does it override the byte[] overload? If it overrides neither there's something wrong,
                    // but that's a different problem — this test is specifically about the span path.
                    if (!overridesSpan)
                    {
                        offenders.Add(type.FullName);
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "These NAudio WaveStream subclasses fall back to the Stream span-bridge (one ArrayPool rent + copy per read). " +
                "Override Read(Span<byte>) directly and have Read(byte[], int, int) forward to it:\n" +
                string.Join("\n", offenders));
        }
    }
}
