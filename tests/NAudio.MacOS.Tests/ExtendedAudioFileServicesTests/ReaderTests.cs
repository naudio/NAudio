
using System;
using System.IO;

using NAudio.Wave;

using NAudio.MacOS.AudioToolbox;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.ExtendedAudioFileServicesTests;

[TestFixture]
public class ReaderTests
{
    private static string GetFilePathFromEnv()
    {
        string path = System.Environment.GetEnvironmentVariable("NAUDIO_MACOS_TESTS_PATH_TO_ANY_AUDIO_FILE");
        if (!File.Exists(path))
        {
            Assert.Ignore("The environment variable NAUDIO_MACOS_TESTS_PATH_TO_ANY_AUDIO_FILE has not been set to a valid audio file");
        }
        return path;
    }

    private static void CreateFilePathReader(ExtendedFileReaderSettings settings = null)
    {
        ReaderCommon(ExtendedAudioFileReaderFromURL.CreateFromFile(GetFilePathFromEnv(), settings));
    }

    private static void CreateStreamBasedReader(ExtendedAudioFileReaderFromStream.ExtendedAudioFileReaderFromStreamSettings settings = null, bool specifyFileNameInSettings = false)
    {
        var fs = new FileStream(GetFilePathFromEnv(), FileMode.Open, FileAccess.Read);

        try
        {
            if (specifyFileNameInSettings)
            {
                settings ??= new();
                settings.FileName = fs.Name;
            }
            ReaderCommon(new ExtendedAudioFileReaderFromStream(fs, settings));
        }
        finally
        {
            fs.Dispose();
        }
    }

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 3, 1);

    [Test]
    public void CanReadARandomFile_DefaultSettings() => CreateFilePathReader();

    [Test]
    public void CanReadARandomFile_StreamCallbacks_DefaultSettings() => CreateStreamBasedReader();

    [Test]
    public void CanReadARandomFile_ReqIEEEFloat() => CreateFilePathReader(new() { RequestIeeeFloat = true });

    [Test]
    public void CanReadARandomFile_StreamCallbacks_ReqIEEEFloat() => CreateStreamBasedReader(new() { RequestIeeeFloat = true });

    [Test]
    public void CanReadARandomFile_CustomFormat() => CreateFilePathReader(new() { OutputFormat = new(48000, 2) });

    [Test]
    public void CanReadARandomFile_StreamCallbacks_CustomFormat() => CreateStreamBasedReader(new() { OutputFormat = new(48000, 2) });

    [Test]
    public void CanReadARandomFile_StreamCallbacks_DefaultSettingsWithNameSpecified() => CreateStreamBasedReader(null, true);

    private static void ReaderCommon(AbstractExtendedFileReader reader)
    {
        // You cannot write to a reader
        Assert.IsFalse(reader.CanWrite);

        var settings = reader.Settings;

        System.Console.WriteLine("Total time: {0}", reader.TotalTime);
        System.Console.WriteLine("Total # of avg.bytes: {0}", reader.Length);
        System.Console.WriteLine("Total # of samples: {0}", reader.LengthInFrames);

        WaveFormat outF = null;
        Assert.DoesNotThrow(() => outF = reader.WaveFormat);

        if (outF is WaveFormatExtensible extFormat)
        {
            System.Console.WriteLine(
                "Reader Format:\n" +
                "Sample Rate: {0}\n" +
                "Channels: {1}\n" +
                "Bits per sample: {2}\n" +
                "Avg. bytes per second: {3}\n" +
                "Block align: {4}\n" +
                "Valid bits per sample: {5}\n" +
                "Channel mask: {6}\n" +
                "Sub format: {7}",
                extFormat.SampleRate,
                extFormat.Channels,
                extFormat.BitsPerSample,
                extFormat.AverageBytesPerSecond,
                extFormat.BlockAlign,
                extFormat.ValidBitsPerSample,
                (Speakers)extFormat.ChannelMask,
                extFormat.SubFormat
            );
        }
        else
        {
            System.Console.WriteLine(
                "Reader Format:\n" +
                "Sample Rate: {0}\n" +
                "Channels: {1}\n" +
                "Bits per sample: {2}\n" +
                "Avg. bytes per second: {3}\n" +
                "Block align: {4}\n" +
                "Encoding: {5}",
                outF.SampleRate,
                outF.Channels,
                outF.BitsPerSample,
                outF.AverageBytesPerSecond,
                outF.BlockAlign,
                outF.Encoding
            );
        }

        if (settings is not null)
        {
            WaveFormat formatFromSettings = settings.OutputFormat;

            if (formatFromSettings is null && settings.RequestIeeeFloat)
            {
                bool throwIeeeError = false;
                if (outF.Encoding == WaveFormatEncoding.Extensible)
                {
                    if (outF is WaveFormatExtensible ext)
                    {
                        throwIeeeError = ext.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
                    }
                    else
                    {
                        Assert.Fail("Expected a WaveFormatExtensible here, but it was " + outF.GetType().FullName);
                    }
                }
                else
                {
                    throwIeeeError = outF.Encoding != WaveFormatEncoding.IeeeFloat;
                }
                if (throwIeeeError)
                {
                    Assert.Fail(
                        "Requested IEEE float while the output format is not IEEE float.\nSpecifically it was: " + outF
                    );
                }
            }

            if (formatFromSettings is not null && !outF.Equals(formatFromSettings))
            {
                Assert.Fail("Actual output format does not match the selected output format.");
            }
        }

        int read;
        long readBytes = 0L;

        byte[] buffer = new byte[reader.WaveFormat.ConvertLatencyToByteSize(300)];

        do
        {
            read = reader.Read(buffer);
            readBytes += read;
        } while (read > 0);

        System.Console.WriteLine("Read bytes: {0}", readBytes);

        // We can have a file with zero samples, so check that first.
        if (reader.LengthInFrames > 0L)
        {
            Assert.Greater(readBytes, 0L);
        }

        // Test whether the stream can be sought.
        Assert.DoesNotThrow(() => reader.PositionInFrames = 0L);

        // Now, attempt to read again.
        read = reader.Read(buffer);

        // The call should succeed, provided that we give a valid audio file that has a length of several minutes.
        Assert.Greater(read, 0);

        // Make a buffer less than BlockAlign to verify that it throws when the buffer is not at least a full sample frame.
        buffer = new byte[reader.WaveFormat.BlockAlign - 1];

        Assert.Throws<ArgumentException>(() => _ = reader.Read(buffer), "Buffers that have a size less than BlockAlign should throw in the read call.");

        // This must not throw any exception
        Assert.DoesNotThrow(reader.Dispose);

        // You cannot read/seek from a disposed reader
        Assert.IsFalse(reader.CanRead);

        Assert.IsFalse(reader.CanSeek);

        // All of the below calls must throw ObjectDisposedException.
        Assert.Throws<ObjectDisposedException>(() => _ = reader.PositionInFrames);

        Assert.Throws<ObjectDisposedException>(() => _ = reader.Position);

        Assert.Throws<ObjectDisposedException>(() => _ = reader.LengthInFrames);

        Assert.Throws<ObjectDisposedException>(() => _ = reader.Length);

        Assert.Throws<ObjectDisposedException>(() => reader.PositionInFrames = 0L);

        Assert.Throws<ObjectDisposedException>(() => reader.Position = 0L);
    }
}