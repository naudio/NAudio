
using System;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NAudio.MacOS.AudioToolbox;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.ExtendedAudioFileServicesTests;

public class WriterTests
{
    private static string CreateRandomFileName() => Path.Join(Environment.CurrentDirectory, Path.GetRandomFileName());

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 3, 1);

    [Test]
    public void WriteAAC() => UseURLWriter(new() { FileType = "audio/aac" });

    [Test]
    public void WriteMPEG4() => UseURLWriter(new() { FileType = "audio/mp4" });

    [Test]
    public void WriteMPEG4ByStreamCallbacks() => UseWriterWithStream(new() { FileType = "audio/mp4" });

    [Test]
    public void WriteAACByStreamCallbacks() => UseWriterWithStream(new() { FileType = "audio/aac" });

    private static void UseURLWriter(ExtendedFileWriterSettings settings)
    {
        var sg = new SignalGenerator(48000, 2).Take(TimeSpan.FromSeconds(5)).ToWaveProvider();
        settings ??= new();
        settings.OutputFormat = sg.WaveFormat;
        var wr = ExtendedAudioFileWriter.CreateFromFilePath(CreateRandomFileName(), settings, true);
        WriteAndDisposeWriter(wr, sg);
    }

    private static void UseWriterWithStream(ExtendedFileWriterSettings settings)
    {
        var sg = new SignalGenerator(48000, 2).Take(TimeSpan.FromSeconds(5)).ToWaveProvider();
        settings ??= new();
        settings.OutputFormat = sg.WaveFormat;

        var fs = new FileStream(
            CreateRandomFileName(),
            FileMode.Create,
            FileAccess.ReadWrite
        );

        try
        {
            var wr = ExtendedAudioFileWriter.CreateFromStream(fs, settings);
            WriteAndDisposeWriter(wr, sg);
        }
        finally
        {
            fs.Dispose();
        }
    }

    private static void WriteAndDisposeWriter(AbstractExtendedFileWriter writer, IWaveProvider provider)
    {
        byte[] buffer = new byte[provider.WaveFormat.ConvertLatencyToByteSize(300)];

        long writtenBytes = 0L;
        int providerRead;

        while ((providerRead = provider.Read(buffer)) > 0)
        {
            writer.Write(buffer.AsSpan(0, providerRead));
            writtenBytes += providerRead;
        }

        System.Console.WriteLine("Written bytes: {0}", writtenBytes);

        Assert.DoesNotThrow(writer.Dispose);
    }
}