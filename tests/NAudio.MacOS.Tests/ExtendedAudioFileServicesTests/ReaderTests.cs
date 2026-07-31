
using System.IO;

using NAudio.Dmo;
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
    public void CanReadARandomFile_ReqIEEEFloat() => CreateFilePathReader();

    [Test]
    public void CanReadARandomFile_StreamCallbacks_ReqIEEEFloat() => CreateStreamBasedReader();

    [Test]
    public void CanReadARandomFile_CustomFormat() => CreateFilePathReader(new() { OutputFormat = new(48000, 2) });

    [Test]
    public void CanReadARandomFile_StreamCallbacks_CustomFormat() => CreateStreamBasedReader(new() { OutputFormat = new(48000, 2) });

    [Test]
    public void CanReadARandomFile_StreamCallbacks_DefaultSettingsWithNameSpecified() => CreateStreamBasedReader(null, true);

    private static void ReaderCommon(AbstractExtendedFileReader reader)
    {
        var settings = reader.Settings;

        System.Console.WriteLine("Total time: {0}", reader.TotalTime);
        System.Console.WriteLine("Total # of avg.bytes: {0}", reader.Length);
        System.Console.WriteLine("Total # of samples: {0}", reader.LengthInFrames);

        WaveFormat outF = null;
        Assert.DoesNotThrow(() => outF = reader.WaveFormat);

        if (settings is not null)
        {
            WaveFormat formatFromSettings = settings.OutputFormat;

            if (formatFromSettings is null && settings.RequestIeeeFloat)
            {
                if (
                    outF.Encoding != WaveFormatEncoding.IeeeFloat ||
                    (outF.Encoding == WaveFormatEncoding.Extensible && outF is WaveFormatExtensible e && e.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
                )
                {
                    Assert.Fail("Requested IEEE float while the output format is not IEEE float.");
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

        // This must not throw any exception
        Assert.DoesNotThrow(reader.Dispose);
    }
}