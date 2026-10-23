using System;
using System.IO;
using System.Text;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sfz;

[TestFixture]
[Category("UnitTest")]
public class SfzFilePathTests
{
    private string root;
    private string programs;
    private string samples;

    [SetUp]
    public void SetUp()
    {
        // <root>/Library/Programs/x.sfz referring to <root>/Library/Samples/c4.wav,
        // plus <root>/outside.wav that a sandboxed resolve must not reach
        root = Path.Combine(Path.GetTempPath(), "sfzpath-" + Guid.NewGuid().ToString("N"));
        programs = Path.Combine(root, "Library", "Programs");
        samples = Path.Combine(root, "Library", "Samples");
        Directory.CreateDirectory(programs);
        Directory.CreateDirectory(samples);
        File.WriteAllBytes(Path.Combine(samples, "c4.wav"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(samples, "empty.wav"), Array.Empty<byte>());
        File.WriteAllBytes(Path.Combine(root, "outside.wav"), new byte[] { 1 });
    }

    [TearDown]
    public void TearDown() => Directory.Delete(root, recursive: true);

    [Test]
    public void ResolvesRelativePathsThatClimbOutOfTheBaseDirectory()
    {
        // the common library layout: sample=..\Samples\c4.wav
        Assert.That(SfzFilePath.Resolve(programs, @"..\Samples\c4.wav"),
            Is.EqualTo(Path.Combine(samples, "c4.wav")));
        Assert.That(SfzFilePath.Resolve(programs, "../Samples/c4.wav"),
            Is.EqualTo(Path.Combine(samples, "c4.wav")));
    }

    [Test]
    public void RefusesAbsolutePathsWithoutARoot()
    {
        Assert.That(SfzFilePath.Resolve(programs, Path.Combine(samples, "c4.wav")), Is.Null);
    }

    [Test]
    public void RefusesNetworkPathsWithoutARoot()
    {
        Assert.That(SfzFilePath.Resolve(programs, @"\\attacker\share\x.wav"), Is.Null);
        Assert.That(SfzFilePath.Resolve(programs, "//attacker/share/x.wav"), Is.Null);
    }

    [Test]
    public void RootAllowsPathsInsideItIncludingAbsoluteOnes()
    {
        var library = Path.Combine(root, "Library");
        Assert.That(SfzFilePath.Resolve(programs, "../Samples/c4.wav", library), Is.Not.Null);
        Assert.That(SfzFilePath.Resolve(programs, Path.Combine(samples, "c4.wav"), library), Is.Not.Null);
    }

    [Test]
    public void RootRefusesPathsOutsideIt()
    {
        var library = Path.Combine(root, "Library");
        Assert.That(SfzFilePath.Resolve(programs, "../../outside.wav", library), Is.Null);
        Assert.That(SfzFilePath.Resolve(programs, Path.Combine(root, "outside.wav"), library), Is.Null);
        // a sibling whose name merely starts with the root's name is still outside it
        Directory.CreateDirectory(library + "2");
        File.WriteAllBytes(Path.Combine(library + "2", "x.wav"), new byte[] { 1 });
        Assert.That(SfzFilePath.Resolve(programs, "../../Library2/x.wav", library), Is.Null);
    }

    [Test]
    public void TreatsEmptyAndMissingFilesAsMissing()
    {
        Assert.That(SfzFilePath.Resolve(samples, "empty.wav"), Is.Null);
        Assert.That(SfzFilePath.Resolve(samples, "nope.wav"), Is.Null);
        Assert.That(SfzFilePath.Resolve(samples, ""), Is.Null);
    }

    [Test]
    public void IncludeResolverRefusesAbsolutePathsByDefault()
    {
        var common = Path.Combine(samples, "common.sfz");
        File.WriteAllText(common, "<region> sample=a.wav");
        Assert.That(new FileSfzIncludeResolver(programs).Resolve(common), Is.Null);
        Assert.That(new FileSfzIncludeResolver(programs).Resolve("../Samples/common.sfz"),
            Is.EqualTo("<region> sample=a.wav"));
        Assert.That(new FileSfzIncludeResolver(programs, root).Resolve(common),
            Is.EqualTo("<region> sample=a.wav"));
    }

    [Test]
    public void IncludeResolverRefusesHugeFiles()
    {
        var huge = Path.Combine(programs, "huge.sfz");
        using (var stream = File.Create(huge))
        {
            stream.SetLength(FileSfzIncludeResolver.MaxIncludeFileBytes + 1);
        }
        Assert.Throws<InvalidDataException>(() => new FileSfzIncludeResolver(programs).Resolve("huge.sfz"));
    }

    [Test]
    public void SelfDoublingDefineIsRefused()
    {
        var sfz = new StringBuilder("#define $a xxxxxxxx\n");
        for (int i = 0; i < 40; i++) sfz.Append("#define $a $a$a\n");
        Assert.Throws<InvalidDataException>(() => SfzParser.Parse(sfz.ToString()));
    }

    [Test]
    public void IncludeFanOutIsRefused()
    {
        // a file that includes itself twice: depth-limited, but 2^32 expansions without a count limit
        var resolver = new SelfIncludingResolver();
        Assert.Throws<InvalidDataException>(() => SfzParser.Parse(SelfIncludingResolver.Text, resolver));
        Assert.That(resolver.Calls, Is.LessThanOrEqualTo(4096));
    }

    private sealed class SelfIncludingResolver : ISfzIncludeResolver
    {
        public const string Text = "#include \"self.sfz\"\n#include \"self.sfz\"\n";
        public int Calls;

        public string Resolve(string path)
        {
            Calls++;
            return Text;
        }
    }
}
