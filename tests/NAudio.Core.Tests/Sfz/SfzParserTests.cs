using System.Collections.Generic;
using System.Linq;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sfz
{
    [TestFixture]
    [Category("UnitTest")]
    public class SfzParserTests
    {
        private sealed class StubIncludeResolver : ISfzIncludeResolver
        {
            private readonly Dictionary<string, string> files;
            public StubIncludeResolver(Dictionary<string, string> files) => this.files = files;
            public string Resolve(string path) => files.TryGetValue(path, out var text) ? text : null;
        }

        [Test]
        public void ParsesASingleRegion()
        {
            var sfz = "<region> sample=kick.wav lokey=36 hikey=36 pitch_keycenter=36";
            var instrument = SfzParser.Parse(sfz);

            Assert.That(instrument.Regions, Has.Count.EqualTo(1));
            var region = instrument.Regions[0];
            Assert.That(region.Sample, Is.EqualTo("kick.wav"));
            Assert.That(region.GetInt("lokey", -1), Is.EqualTo(36));
            Assert.That(region.GetInt("hikey", -1), Is.EqualTo(36));
        }

        [Test]
        public void GlobalAndGroupOpcodesMergeIntoRegionsWithRegionWinning()
        {
            var sfz = @"
<global> volume=-3 ampeg_release=0.5
<group> cutoff=2000 volume=-6
<region> sample=a.wav
<region> sample=b.wav cutoff=500";
            var instrument = SfzParser.Parse(sfz);

            Assert.That(instrument.Regions, Has.Count.EqualTo(2));

            var a = instrument.Regions[0];
            Assert.That(a.GetFloat("ampeg_release", 0), Is.EqualTo(0.5f)); // from global
            Assert.That(a.GetFloat("volume", 0), Is.EqualTo(-6f));         // group beats global
            Assert.That(a.GetInt("cutoff", 0), Is.EqualTo(2000));          // from group

            var b = instrument.Regions[1];
            Assert.That(b.GetInt("cutoff", 0), Is.EqualTo(500));           // region beats group
        }

        [Test]
        public void NewGroupResetsTheGroupScope()
        {
            var sfz = @"
<group> cutoff=1000
<region> sample=a.wav
<group> resonance=3
<region> sample=b.wav";
            var instrument = SfzParser.Parse(sfz);

            Assert.That(instrument.Regions[0].GetInt("cutoff", -1), Is.EqualTo(1000));
            // second region is under a new group that does not carry the old cutoff
            Assert.That(instrument.Regions[1].Has("cutoff"), Is.False);
            Assert.That(instrument.Regions[1].GetInt("resonance", -1), Is.EqualTo(3));
        }

        [Test]
        public void MasterAppliesUntilNextMasterAndResetsGroup()
        {
            var sfz = @"
<master> pan=-50
<group> cutoff=1000
<region> sample=a.wav
<master> pan=50
<region> sample=b.wav";
            var instrument = SfzParser.Parse(sfz);

            var a = instrument.Regions[0];
            Assert.That(a.GetInt("pan", 0), Is.EqualTo(-50));
            Assert.That(a.GetInt("cutoff", -1), Is.EqualTo(1000));

            var b = instrument.Regions[1];
            Assert.That(b.GetInt("pan", 0), Is.EqualTo(50)); // new master
            Assert.That(b.Has("cutoff"), Is.False);          // group reset by new master
        }

        [Test]
        public void SamplePathsMayContainSpaces()
        {
            var sfz = "<region> sample=Grand Piano C4.wav volume=-3";
            var region = SfzParser.Parse(sfz).Regions[0];
            Assert.That(region.Sample, Is.EqualTo("Grand Piano C4.wav"));
            Assert.That(region.GetFloat("volume", 0), Is.EqualTo(-3f));
        }

        [Test]
        public void DefaultPathIsAppliedToSamplePaths()
        {
            var sfz = @"
<control> default_path=Samples\Piano\
<region> sample=C4.wav";
            var instrument = SfzParser.Parse(sfz);
            Assert.That(instrument.DefaultPath, Is.EqualTo("Samples/Piano/"));
            Assert.That(instrument.Regions[0].Sample, Is.EqualTo("Samples/Piano/C4.wav"));
        }

        [Test]
        public void BackslashesInSamplePathsAreNormalised()
        {
            var region = SfzParser.Parse(@"<region> sample=sub\dir\snare.wav").Regions[0];
            Assert.That(region.Sample, Is.EqualTo("sub/dir/snare.wav"));
        }

        [Test]
        public void LineAndBlockCommentsAreStripped()
        {
            var sfz = @"
// a leading comment
<region> sample=a.wav // trailing comment
/* block
   comment */ volume=-6";
            var region = SfzParser.Parse(sfz).Regions[0];
            Assert.That(region.Sample, Is.EqualTo("a.wav"));
            Assert.That(region.GetFloat("volume", 0), Is.EqualTo(-6f));
        }

        [Test]
        public void DefineAndVariableSubstitution()
        {
            var sfz = @"
#define $KEY 60
#define $REL 0.8
<region> sample=a.wav lokey=$KEY hikey=$KEY ampeg_release=$REL";
            var region = SfzParser.Parse(sfz).Regions[0];
            Assert.That(region.GetInt("lokey", -1), Is.EqualTo(60));
            Assert.That(region.GetInt("hikey", -1), Is.EqualTo(60));
            Assert.That(region.GetFloat("ampeg_release", 0), Is.EqualTo(0.8f));
        }

        [Test]
        public void IncludePullsInAnotherFile()
        {
            var resolver = new StubIncludeResolver(new Dictionary<string, string>
            {
                ["common.sfz"] = "<global> ampeg_release=1.0"
            });
            var sfz = "#include \"common.sfz\"\n<region> sample=a.wav";
            var region = SfzParser.Parse(sfz, resolver).Regions[0];
            Assert.That(region.GetFloat("ampeg_release", 0), Is.EqualTo(1.0f));
        }

        [Test]
        public void DefinesCrossIntoIncludedFiles()
        {
            var resolver = new StubIncludeResolver(new Dictionary<string, string>
            {
                ["region.sfz"] = "<region> sample=a.wav lokey=$K hikey=$K"
            });
            var sfz = "#define $K 48\n#include \"region.sfz\"";
            var region = SfzParser.Parse(sfz, resolver).Regions[0];
            Assert.That(region.GetInt("lokey", -1), Is.EqualTo(48));
        }

        [Test]
        public void OpcodesBeforeAnyHeaderBecomeAGlobal()
        {
            var sfz = "ampeg_release=2.0\n<region> sample=a.wav";
            var region = SfzParser.Parse(sfz).Regions[0];
            Assert.That(region.GetFloat("ampeg_release", 0), Is.EqualTo(2.0f));
        }

        [Test]
        public void EmptyInputHasNoRegions()
        {
            Assert.That(SfzParser.Parse("").Regions, Is.Empty);
            Assert.That(SfzParser.Parse("   \n  // just a comment\n").Regions, Is.Empty);
        }

        [Test]
        public void NoteAndOctaveOffsetsAreCaptured()
        {
            var sfz = "<control> note_offset=2 octave_offset=-1\n<region> sample=a.wav";
            var instrument = SfzParser.Parse(sfz);
            Assert.That(instrument.NoteOffset, Is.EqualTo(2));
            Assert.That(instrument.OctaveOffset, Is.EqualTo(-1));
        }

        [Test]
        public void SetCcInitialControllerValuesAreCaptured()
        {
            var sfz = "<control> set_cc20=100 set_cc7=90\n<region> sample=a.wav";
            var instrument = SfzParser.Parse(sfz);
            Assert.That(instrument.InitialControllerValues, Has.Count.EqualTo(2));
            Assert.That(instrument.InitialControllerValues, Has.Member((20, 100)));
            Assert.That(instrument.InitialControllerValues, Has.Member((7, 90)));

            Assert.That(SfzParser.Parse("<region> sample=a.wav").InitialControllerValues, Is.Null);
        }

        [Test]
        public void SetCcLaterControlSectionsWinAndMalformedEntriesAreIgnored()
        {
            var sfz = @"
<control> set_cc20=10 set_cc999=64 set_ccx=64 set_cc7=200
<control> set_cc20=90
<region> sample=a.wav";
            var instrument = SfzParser.Parse(sfz);
            Assert.That(instrument.InitialControllerValues, Has.Count.EqualTo(2));
            Assert.That(instrument.InitialControllerValues, Has.Member((20, 90)), "the later section wins");
            Assert.That(instrument.InitialControllerValues, Has.Member((7, 127)), "values clamp to MIDI 0..127");
        }

        [Test]
        public void SectionsArePreservedInOrder()
        {
            var sfz = "<global> volume=0\n<group> pan=0\n<region> sample=a.wav";
            var headers = SfzParser.Parse(sfz).Sections.Select(s => s.Header).ToArray();
            Assert.That(headers, Is.EqualTo(new[] { SfzHeader.Global, SfzHeader.Group, SfzHeader.Region }));
        }

        [Test]
        public void HeaderAdjacentToAnOpcodeIsStillAHeader()
        {
            // real players treat '<' as a delimiter wherever it appears, so a
            // header written with no whitespace before its first opcode must not
            // collapse into a junk opcode key (silently dropping the region)
            var instrument = SfzParser.Parse("<region>sample=a.wav");
            Assert.That(instrument.Regions, Has.Count.EqualTo(1));
            Assert.That(instrument.Regions[0].Sample, Is.EqualTo("a.wav"));
        }

        [Test]
        public void AdjacentHeadersAreSeparateSections()
        {
            var instrument = SfzParser.Parse("<group><region>sample=a.wav");
            Assert.That(instrument.Sections.Select(s => s.Header),
                Is.EqualTo(new[] { SfzHeader.Group, SfzHeader.Region }));
            Assert.That(instrument.Regions, Has.Count.EqualTo(1));
            Assert.That(instrument.Regions[0].Sample, Is.EqualTo("a.wav"));
        }

        [Test]
        public void MixedLineWithUnspacedHeadersParsesHeadersAndOpcodes()
        {
            var instrument = SfzParser.Parse("<group>key=36 <region>sample=a.wav");
            Assert.That(instrument.Regions, Has.Count.EqualTo(1));
            var region = instrument.Regions[0];
            Assert.That(region.Sample, Is.EqualTo("a.wav"));
            Assert.That(region.GetInt("key", -1), Is.EqualTo(36)); // inherited from the group
        }

        [Test]
        public void DefaultPathChangesApplyToSubsequentRegionsInOrder()
        {
            var sfz = @"
<control> default_path=a/
<region> sample=one.wav
<control> default_path=b/
<region> sample=two.wav";
            var regions = SfzParser.Parse(sfz).Regions;
            Assert.That(regions[0].Sample, Is.EqualTo("a/one.wav"));
            Assert.That(regions[1].Sample, Is.EqualTo("b/two.wav"));
        }
    }
}
