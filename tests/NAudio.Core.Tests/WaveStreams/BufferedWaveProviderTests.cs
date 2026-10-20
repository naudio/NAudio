using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class BufferedWaveProviderTests
{
    [Test]
    public void CanClearBeforeWritingSamples()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.ClearBuffer();
        Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
    }

    [Test]
    public void BufferedBytesAreReturned()
    {
        var bytesToBuffer = 1000;
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        var data = Enumerable.Range(1, bytesToBuffer).Select(n => (byte)(n % 256)).ToArray();
        bwp.AddSamples(data, 0, data.Length);
        Assert.That(bwp.BufferedBytes, Is.EqualTo(bytesToBuffer));
        var readBuffer = new byte[bytesToBuffer];
        var bytesRead = bwp.Read(readBuffer.AsSpan());
        Assert.That(bytesRead, Is.EqualTo(bytesToBuffer));
        Assert.That(readBuffer, Is.EqualTo(data));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
    }

    [Test]
    public void EmptyBufferCanReturnZeroFromRead()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat());
        bwp.ReadFully = false;
        var buffer = new byte[44100];
        var read = bwp.Read(buffer.AsSpan());
        Assert.That(read, Is.EqualTo(0));
    }

    [Test]
    public void PartialReadsPossibleWithReadFullyFalse()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat());
        bwp.ReadFully = false;
        var buffer = new byte[44100];
        bwp.AddSamples(buffer, 0, 2000);
        var read = bwp.Read(buffer.AsSpan());
        Assert.That(read, Is.EqualTo(2000));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
    }

    [Test]
    public void FullReadsByDefault()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat());
        var buffer = new byte[44100];
        bwp.AddSamples(buffer, 0, 2000);
        var read = bwp.Read(buffer.AsSpan());
        Assert.That(read, Is.EqualTo(buffer.Length));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
    }

    [Test]
    public void WhenBufferHasMoreThanNeededReadFully()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat());
        var buffer = new byte[44100];
        bwp.AddSamples(buffer, 0, 5000);
        var read = bwp.Read(buffer.AsSpan(0, 2000));
        Assert.That(read, Is.EqualTo(2000));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(3000));
    }

    // ── Constructor defaults ──────────────────────────────────────────────

    [Test]
    public void DefaultBufferLengthIsFiveSeconds()
    {
        var format = new WaveFormat(44100, 16, 2);
        var bwp = new BufferedWaveProvider(format);
        Assert.That(bwp.BufferLength, Is.EqualTo(format.AverageBytesPerSecond * 5));
    }

    [Test]
    public void ReadFullyIsTrueByDefault()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat());
        Assert.That(bwp.ReadFully, Is.True);
    }

    [Test]
    public void WaveFormatPropertyReturnsConstructorArgument()
    {
        var format = new WaveFormat(44100, 16, 2);
        var bwp = new BufferedWaveProvider(format);
        Assert.That(bwp.WaveFormat, Is.SameAs(format));
    }

    // ── BufferLength property ─────────────────────────────────────────────

    [Test]
    public void BufferLengthReflectsConstructorDuration()
    {
        var format = new WaveFormat(44100, 16, 2);
        var bwp = new BufferedWaveProvider(format, TimeSpan.FromSeconds(2));
        Assert.That(bwp.BufferLength, Is.EqualTo((int)(2.0 * format.AverageBytesPerSecond)));
    }

    // ── BufferDuration property ───────────────────────────────────────────

    [Test]
    public void BufferDurationReflectsConstructorArgument()
    {
        var format = new WaveFormat(44100, 16, 2);
        var bwp = new BufferedWaveProvider(format, TimeSpan.FromSeconds(3));
        Assert.That(bwp.BufferDuration.TotalSeconds, Is.EqualTo(3.0).Within(0.001));
    }

    // ── DiscardOnBufferOverflow ───────────────────────────────────────────

    [Test]
    public void AddSamplesThrowsWhenBufferFullAndDiscardDisabled()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100));
        bwp.AddSamples(new byte[bwp.BufferLength], 0, bwp.BufferLength);
        Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[1], 0, 1));
    }

    [Test]
    public void AddSamplesDiscardsWhenBufferFullAndDiscardEnabled()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100));
        bwp.DiscardOnBufferOverflow = true;
        bwp.AddSamples(new byte[bwp.BufferLength], 0, bwp.BufferLength);
        Assert.DoesNotThrow(() => bwp.AddSamples(new byte[100], 0, 100));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(bwp.BufferLength));
    }

    // ── ClearBuffer ───────────────────────────────────────────────────────

    [Test]
    public void ClearBufferResetsBufferedBytesToZero()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[500], 0, 500);
        bwp.ClearBuffer();
        Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
    }

    // ── Offset parameters ─────────────────────────────────────────────────

    [Test]
    public void AddSamplesRespectsSourceOffset()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.ReadFully = false;
        bwp.AddSamples(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 4, 4);
        var readBuffer = new byte[4];
        bwp.Read(readBuffer.AsSpan());
        Assert.That(readBuffer, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
    }

    [Test]
    public void ReadRespectsDestinationOffset()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.ReadFully = false;
        bwp.AddSamples(new byte[] { 1, 2, 3, 4 }, 0, 4);
        var readBuffer = new byte[8];
        var bytesRead = bwp.Read(readBuffer.AsSpan(4, 4));
        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(readBuffer, Is.EqualTo(new byte[] { 0, 0, 0, 0, 1, 2, 3, 4 }));
    }

    // ── Miscellaneous ─────────────────────────────────────────────────────

    [Test]
    public void MultipleAddSamplesCallsAccumulate()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[200], 0, 200);
        bwp.AddSamples(new byte[300], 0, 300);
        Assert.That(bwp.BufferedBytes, Is.EqualTo(500));
    }

    [Test]
    public void ReadFullyZeroFillsBufferWhenNothingEverWritten()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        var buffer = new byte[] { 1, 2, 3, 4 };
        var bytesRead = bwp.Read(buffer.AsSpan());
        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    // ── CompleteAdding ────────────────────────────────────────────────────

    [TestCase(true)]
    [TestCase(false)]
    public void AfterCompleteAddingReadsDrainThenShortReadThenZero(bool readFully)
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2)) { ReadFully = readFully };
        var data = Enumerable.Range(1, 2500).Select(n => (byte)(n % 255 + 1)).ToArray();
        bwp.AddSamples(data, 0, data.Length);
        bwp.CompleteAdding();
        Assert.That(bwp.IsAddingCompleted, Is.True);

        var buffer = Enumerable.Repeat((byte)0xEE, 1000).ToArray();
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        buffer.AsSpan().Fill(0xEE);
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(500));
        Assert.That(buffer.Take(500), Is.EqualTo(data.Skip(2000)));
        // the short read is not padded: the rest of the buffer is untouched
        Assert.That(buffer.Skip(500), Is.All.EqualTo((byte)0xEE));
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(0));
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(0));
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(0));
    }

    [Test]
    public void ReadsAreStillPaddedBeforeCompleteAdding()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[500], 0, 500);
        Assert.That(bwp.IsAddingCompleted, Is.False);
        Assert.That(bwp.Read(new byte[1000].AsSpan()), Is.EqualTo(1000));
    }

    [Test]
    public void CompleteAddingWithExactlyBufferedAmountEndsWithZero()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[2000], 0, 2000);
        bwp.CompleteAdding();
        var buffer = new byte[1000];
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(1000));
        Assert.That(bwp.Read(buffer.AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void CompleteAddingWhenEmptyReturnsZero()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.CompleteAdding();
        Assert.That(bwp.Read(new byte[1000].AsSpan()), Is.EqualTo(0));
    }

    [Test]
    public void AddSamplesAfterCompleteAddingThrows()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[100], 0, 100);
        bwp.CompleteAdding();
        Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[100], 0, 100));
        Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[100].AsSpan()));
        Assert.That(bwp.BufferedBytes, Is.EqualTo(100));
    }

    [Test]
    public void CompleteAddingCanBeCalledTwice()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.CompleteAdding();
        Assert.DoesNotThrow(() => bwp.CompleteAdding());
        Assert.That(bwp.IsAddingCompleted, Is.True);
    }

    [Test]
    public void ClearBufferDoesNotResetCompletion()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        bwp.AddSamples(new byte[2000], 0, 2000);
        bwp.CompleteAdding();
        bwp.ClearBuffer();
        Assert.That(bwp.IsAddingCompleted, Is.True);
        Assert.That(bwp.Read(new byte[1000].AsSpan()), Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[100], 0, 100));
    }

    // ── Counters ──────────────────────────────────────────────────────────

    [Test]
    public void UnderrunSilenceBytesCountsPadding()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        var buffer = new byte[1000];
        bwp.Read(buffer.AsSpan()); // nothing buffered yet
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(1000));
        bwp.AddSamples(new byte[300], 0, 300);
        bwp.Read(buffer.AsSpan());
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(1700));
        bwp.AddSamples(new byte[1000], 0, 1000);
        bwp.Read(buffer.AsSpan()); // a full read adds nothing
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(1700));
    }

    [Test]
    public void UnderrunSilenceBytesNotCountedWithoutReadFully()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2)) { ReadFully = false };
        bwp.AddSamples(new byte[300], 0, 300);
        Assert.That(bwp.Read(new byte[1000].AsSpan()), Is.EqualTo(300));
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(0));
    }

    [Test]
    public void DiscardedBytesCountsOverflow()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100))
        {
            DiscardOnBufferOverflow = true
        };
        bwp.AddSamples(new byte[bwp.BufferLength - 100], 0, bwp.BufferLength - 100);
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(0));
        bwp.AddSamples(new byte[250], 0, 250);
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(150));
        bwp.AddSamples(new byte[40], 0, 40);
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(190));
    }

    [Test]
    public void DiscardedBytesNotCountedWhenOverflowThrows()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100));
        bwp.AddSamples(new byte[bwp.BufferLength], 0, bwp.BufferLength);
        Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[10], 0, 10));
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(0));
    }

    [Test]
    public void ResetCountersZeroesBothCounters()
    {
        var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100))
        {
            DiscardOnBufferOverflow = true
        };
        bwp.Read(new byte[100].AsSpan());
        bwp.AddSamples(new byte[bwp.BufferLength + 10], 0, bwp.BufferLength + 10);
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(100));
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(10));
        bwp.ResetCounters();
        Assert.That(bwp.UnderrunSilenceBytes, Is.EqualTo(0));
        Assert.That(bwp.DiscardedBytes, Is.EqualTo(0));
    }

    [Test]
    public void CountersAreExactWithConcurrentProducerAndConsumer()
    {
        // Real audio is all 1s and padding is 0, so every byte the producer offered is either
        // read back as a 1 or counted as discarded, and every 0 the consumer saw is counted as
        // underrun silence.
        var bwp = new BufferedWaveProvider(new WaveFormat(8000, 8, 1), TimeSpan.FromMilliseconds(50))
        {
            DiscardOnBufferOverflow = true
        };
        var chunk = Enumerable.Repeat((byte)1, 333).ToArray();
        var random = new Random(1);
        long added = 0;
        void Produce(int chunks)
        {
            for (int i = 0; i < chunks; i++)
            {
                int count = random.Next(1, chunk.Length + 1);
                bwp.AddSamples(chunk, 0, count);
                added += count;
                if (random.Next(4) == 0) Thread.Yield();
            }
        }

        // overflow the 400-byte buffer before the consumer starts, so discards are certain
        Produce(10);

        long returned = 0, audio = 0;
        int shortReads = 0, readsAfterShortRead = 0;
        var readBuffer = new byte[257];
        var consumer = Task.Run(() =>
        {
            int read;
            while ((read = bwp.Read(readBuffer.AsSpan())) > 0)
            {
                if (shortReads > 0) readsAfterShortRead++;
                if (read < readBuffer.Length) shortReads++;
                returned += read;
                for (int i = 0; i < read; i++) audio += readBuffer[i];
            }
        });

        var producer = Task.Run(() => Produce(3000));
        Assert.That(producer.Wait(TimeSpan.FromSeconds(30)), Is.True);
        // let the consumer run dry before completing, so underruns are certain
        var underrunBeforeDrain = bwp.UnderrunSilenceBytes;
        Assert.That(SpinWait.SpinUntil(() => bwp.BufferedBytes == 0 && bwp.UnderrunSilenceBytes > underrunBeforeDrain,
            TimeSpan.FromSeconds(10)), Is.True);
        bwp.CompleteAdding();
        Assert.That(consumer.Wait(TimeSpan.FromSeconds(30)), Is.True, "stream did not end after CompleteAdding");

        Assert.That(readsAfterShortRead, Is.EqualTo(0), "data returned after a short read");
        Assert.That(shortReads, Is.LessThanOrEqualTo(1));
        Assert.That(audio + bwp.DiscardedBytes, Is.EqualTo(added));
        Assert.That(returned - audio, Is.EqualTo(bwp.UnderrunSilenceBytes));
        Assert.That(bwp.DiscardedBytes, Is.GreaterThan(0));
        Assert.That(bwp.UnderrunSilenceBytes, Is.GreaterThan(0));
    }
}
