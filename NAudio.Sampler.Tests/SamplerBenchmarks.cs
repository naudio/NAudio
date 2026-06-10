using System;
using System.Diagnostics;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Manual throughput/allocation benchmarks for the sampler render path.
    /// Excluded from normal test runs (<see cref="ExplicitAttribute"/>) so CI
    /// times are unaffected; run by hand in Release when working on the engine:
    /// <code>
    /// dotnet test --project NAudio.Sampler.Tests/NAudio.Sampler.Tests.csproj \
    ///   -c Release --filter "TestCategory=Performance"
    /// </code>
    /// Numbers are written to the test output. The assertions are deliberately
    /// loose sanity checks (the point is the printed measurements, not a gate).
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    [Explicit("Manual performance measurement; run in Release")]
    public class SamplerBenchmarks
    {
        private const int SampleRate = 44100;

        // a looped sine with an engaged filter, both LFOs and a reverb send — a
        // deliberately busy voice so the benchmark exercises the whole chain
        private static NAudio.SoundFont.SoundFont BuildBusyFont()
        {
            const int points = 256;
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++)
            {
                short val = (short)(Math.Sin(2 * Math.PI * i / points) * 12000);
                data[i * 2] = (byte)(val & 0xFF);
                data[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(8, 6000),                              // initialFilterFc: filter engaged
                SoundFontTestBuilder.Gen(6, 25),                                // vibLfoToPitch
                SoundFontTestBuilder.Gen(24, unchecked((ushort)(short)-1200)),  // freqVibLfo ~4 Hz
                SoundFontTestBuilder.Gen(10, 1200),                             // modLfoToFilterFc
                SoundFontTestBuilder.Gen(91, 200),                              // reverb send 20%
                SoundFontTestBuilder.Gen(54, 1),                                // loop
                SoundFontTestBuilder.Gen(58, 60),                               // root key
                SoundFontTestBuilder.Gen(53, 0)));                              // sampleID
            return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, points, 0, points, SampleRate, 60);
        }

        [Test]
        public void RenderThroughput64Voices()
        {
            var sampler = new SoundFontSampler(BuildBusyFont(), SampleRate, maxVoices: 64)
            {
                PercussionChannel = -1
            };

            // 64 sustained voices spread over all 16 channels
            for (int v = 0; v < 64; v++) sampler.NoteOn(v % 16, 36 + v % 49, 100);

            var buffer = new float[1024 * 2];
            for (int i = 0; i < 100; i++) sampler.Read(buffer); // JIT + cache warm-up

            const double seconds = 20.0;
            long frames = (long)(seconds * SampleRate);
            long done = 0;
            long alloc0 = GC.GetAllocatedBytesForCurrentThread();
            int gen0Before = GC.CollectionCount(0);
            var sw = Stopwatch.StartNew();
            while (done < frames)
            {
                sampler.Read(buffer);
                done += 1024;
            }
            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - alloc0;
            int gen0 = GC.CollectionCount(0) - gen0Before;

            double xRealtime = seconds / sw.Elapsed.TotalSeconds;
            TestContext.Out.WriteLine(
                $"steady-state: {sampler.ActiveVoiceCount} voices, {seconds:F0} s audio in " +
                $"{sw.Elapsed.TotalSeconds:F2} s = {xRealtime:F1}x realtime; " +
                $"{allocated} B allocated ({allocated / seconds:F0} B/s), {gen0} Gen0 GCs");

            Assert.That(sampler.ActiveVoiceCount, Is.GreaterThan(0));
            Assert.That(xRealtime, Is.GreaterThan(1.0), "64 busy voices should render faster than realtime");
        }

        [Test]
        public void NoteChurnAllocations()
        {
            var sampler = new SoundFontSampler(BuildBusyFont(), SampleRate, maxVoices: 32)
            {
                PercussionChannel = -1
            };
            var buffer = new float[512 * 2];

            // prime caches, pools and JIT, including the note-on path
            for (int i = 0; i < 200; i++)
            {
                int ch = i % 16, note = 40 + i % 40;
                sampler.NoteOn(ch, note, 100);
                sampler.Read(buffer);
                sampler.NoteOff(ch, note);
            }

            const int cycles = 1000;
            long alloc0 = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < cycles; i++)
            {
                int ch = i % 16, note = 40 + i % 40;
                sampler.NoteOn(ch, note, 100);
                sampler.Read(buffer);
                sampler.NoteOff(ch, note);
            }
            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - alloc0;

            TestContext.Out.WriteLine(
                $"note churn: {cycles} note-on/render/note-off cycles in {sw.Elapsed.TotalMilliseconds:F0} ms; " +
                $"{allocated} B allocated ({(double)allocated / cycles:F1} B/cycle)");

            Assert.That(sampler.ActiveVoiceCount, Is.GreaterThanOrEqualTo(0));
        }
    }
}
