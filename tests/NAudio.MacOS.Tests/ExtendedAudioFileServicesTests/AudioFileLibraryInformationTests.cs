
using System.Collections.Immutable;

using NAudio.MacOS.AudioToolbox;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.ExtendedAudioFileServicesTests;

/// <summary>
/// Smoke tests for the <see cref="AudioFileLibraryInformation"/> class. <br />
/// These tests are not meant to be exhaustive, but rather to ensure that the
/// wrappers are functioning correctly and they do return valid values.
/// </summary>
[TestFixture]
public class AudioFileLibraryInformationTests
{
    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 3);

    [Test]
    public void VerifyThatSupportedMimeTypesReturnsValidValue()
    {
        string[] value = null;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.SupportedMimeTypes);
        Assert.That(value, Is.Not.Null);
        if (value.Length == 0)
        {
            Assert.Warn("The supported MIME types array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Supported MIME types:");
        foreach (var mimeType in value)
        {
            System.Console.WriteLine(mimeType);
            Assert.That(mimeType, Is.Not.Null.And.Not.Empty, "The supported MIME types array contains a null or empty value. This should not happen.");
        }
    }

    [Test]
    public void VerifyThatRecognizedFileExtensionsReturnsValidValue()
    {
        string[] value = null;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.RecognizedFileExtensions);
        Assert.That(value, Is.Not.Null);
        if (value.Length == 0)
        {
            Assert.Warn("The recognized file extensions array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Recognized file extensions:");
        foreach (var extension in value)
        {
            System.Console.WriteLine(extension);
            Assert.That(extension, Is.Not.Null.And.Not.Empty, "The recognized file extensions array contains a null or empty value. This should not happen.");
        }
    }

    [Test]
    public void VerifyThatSupportedReadableMimeTypesReturnsValidValue()
    {
        ImmutableArray<string> value = default;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.SupportedReadableMimeTypes);
        if (value.Length == 0)
        {
            Assert.Warn("The readable MIME types array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Supported readable MIME types:");
        foreach (var mimeType in value)
        {
            System.Console.WriteLine(mimeType);
            Assert.That(mimeType, Is.Not.Null.And.Not.Empty, "The supported readable MIME types array contains a null or empty value. This should not happen.");
        }
    }

    [Test]
    public void VerifyThatSupportedWritableMimeTypesReturnsValidValue()
    {
        ImmutableArray<string> value = default;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.SupportedWritableMimeTypes);
        if (value.Length == 0)
        {
            Assert.Warn("The writable MIME types array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Supported writable MIME types:");
        foreach (var mimeType in value)
        {
            System.Console.WriteLine(mimeType);
            Assert.That(mimeType, Is.Not.Null.And.Not.Empty, "The supported writable MIME types array contains a null or empty value. This should not happen.");
        }
    }

    [Test]
    public void VerifyThatRecognizedReadableFileExtensionsReturnsValidValue()
    {
        ImmutableArray<string> value = default;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.RecognizedReadableFileExtensions);
        if (value.Length == 0)
        {
            Assert.Warn("The recognized readable file extensions array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Recognized readable file extensions:");
        foreach (var extension in value)
        {
            System.Console.WriteLine(extension);
            Assert.That(extension, Is.Not.Null.And.Not.Empty, "The recognized readable file extensions array contains a null or empty value. This should not happen.");
        }
    }

    [Test]
    public void VerifyThatRecognizedWritableFileExtensionsReturnsValidValue()
    {
        ImmutableArray<string> value = default;
        Assert.DoesNotThrow(() => value = AudioFileLibraryInformation.RecognizedWritableFileExtensions);
        if (value.Length == 0)
        {
            Assert.Warn("The recognized writable file extensions array contains no values. This may be valid, but it is unexpected.");
        }
        System.Console.WriteLine("Recognized writable file extensions:");
        foreach (var extension in value)
        {
            System.Console.WriteLine(extension);
            Assert.That(extension, Is.Not.Null.And.Not.Empty, "The recognized writable file extensions array contains a null or empty value. This should not happen.");
        }
    }
}