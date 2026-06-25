using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Midi;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing;

[TestFixture]
[Category("UnitTest")]
public class MidiFileSequenceTests
{
    private readonly List<string> tempFiles = new();

    [TearDown]
    public void Cleanup()
    {
        foreach (var path in tempFiles)
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
        tempFiles.Clear();
    }

    private string WriteMidiFile(Action<IList<MidiEvent>> build, int ppq = 480)
    {
        var col = new MidiEventCollection(0, ppq);
        var track = col.AddTrack();
        build(track);
        col.PrepareForExport(); // sorts and appends the end-of-track marker
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".mid");
        MidiFile.Export(path, col);
        tempFiles.Add(path);
        return path;
    }

    [Test]
    public void NoTimeSignatureEvents_DefaultsToFourFourFromZero()
    {
        var path = WriteMidiFile(track => track.Add(new NoteOnEvent(0, 1, 60, 100, 10)));

        var seq = MidiFileSequence.FromFile(path);

        Assert.That(seq.TimeSignatureMap, Is.Not.Null);
        Assert.That(seq.TimeSignatureMap.SignatureAt(0), Is.EqualTo(TimeSignature.FourFour));
    }

    [Test]
    public void TimeSignatureEvent_ConvertsPowerOfTwoDenominator()
    {
        // NAudio stores the denominator as the exponent: 3 => 2^3 = 8, so this is 6/8.
        var path = WriteMidiFile(track =>
        {
            track.Add(new TimeSignatureEvent(0, 6, 3, 24, 8));
            track.Add(new NoteOnEvent(0, 1, 60, 100, 10));
        });

        var seq = MidiFileSequence.FromFile(path);

        var signature = seq.TimeSignatureMap.SignatureAt(0);
        Assert.That(signature.Numerator, Is.EqualTo(6));
        Assert.That(signature.Denominator, Is.EqualTo(8));
    }

    [Test]
    public void TimeSignatureChangeOnBarBoundary_AppliesAfterTheBoundary()
    {
        // 4/4 from 0, switching to 3/4 after one 4/4 bar (4 beats = 4 * 480 file ticks).
        const int ppq = 480;
        var path = WriteMidiFile(track =>
        {
            track.Add(new TimeSignatureEvent(0, 4, 2, 24, 8));      // 4/4
            track.Add(new TimeSignatureEvent(4 * ppq, 3, 2, 24, 8)); // 3/4 at bar 2
            track.Add(new NoteOnEvent(0, 1, 60, 100, 10));
        }, ppq);

        var seq = MidiFileSequence.FromFile(path);

        // canonical ticks: one 4/4 bar is 4 * CanonicalPpq
        long changeTick = 4L * MusicalTime.CanonicalPpq;
        Assert.That(seq.TimeSignatureMap.SignatureAt(changeTick - 1), Is.EqualTo(new TimeSignature(4, 4)));
        Assert.That(seq.TimeSignatureMap.SignatureAt(changeTick), Is.EqualTo(new TimeSignature(3, 4)));
    }

    [Test]
    public void TimeSignatureChangeOffBarBoundary_FallsBackToInitialSignature()
    {
        // A change one beat in (not a bar boundary) is rejected by TimeSignatureMap; the loader must
        // not throw, falling back to the initial signature for the whole file.
        const int ppq = 480;
        string path = null;
        Assert.DoesNotThrow(() =>
        {
            path = WriteMidiFile(track =>
            {
                track.Add(new TimeSignatureEvent(0, 4, 2, 24, 8));   // 4/4
                track.Add(new TimeSignatureEvent(ppq, 7, 3, 24, 8)); // 7/8 one beat in — off boundary
                track.Add(new NoteOnEvent(0, 1, 60, 100, 10));
            }, ppq);
        });

        MidiFileSequence seq = null;
        Assert.DoesNotThrow(() => seq = MidiFileSequence.FromFile(path));
        Assert.That(seq.TimeSignatureMap.SignatureAt(10L * MusicalTime.CanonicalPpq),
            Is.EqualTo(new TimeSignature(4, 4)));
    }
}
