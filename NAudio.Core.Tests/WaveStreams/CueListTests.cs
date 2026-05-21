using NAudio.Wave;
using NUnit.Framework;
using System;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class CueListTests
    {
        [Test]
        [Category("UnitTest")]
        public void CanCreateEmptyCueList()
        {
            // arrange & act
            var cueList = new CueList();

            // assert
            Assert.That(cueList.Count, Is.EqualTo(0));
        }

        [Test]
        [Category("UnitTest")]
        public void CanAddCueToList()
        {
            // arrange
            var cueList = new CueList();
            var cue = new Cue(1000, "TestLabel");

            // act
            cueList.Add(cue);

            // assert
            Assert.That(cueList.Count, Is.EqualTo(1));
            Assert.That(cueList[0].Position, Is.EqualTo(1000));
            Assert.That(cueList[0].Label, Is.EqualTo("TestLabel"));
        }

        [Test]
        [Category("UnitTest")]
        public void CuePositionsReturnsCorrectArray()
        {
            // arrange
            var cueList = new CueList();
            cueList.Add(new Cue(100, "Cue1"));
            cueList.Add(new Cue(200, "Cue2"));
            cueList.Add(new Cue(300, "Cue3"));

            // act
            var positions = cueList.CuePositions;

            // assert
            Assert.That(positions, Has.Length.EqualTo(3));
            Assert.That(positions[0], Is.EqualTo(100));
            Assert.That(positions[1], Is.EqualTo(200));
            Assert.That(positions[2], Is.EqualTo(300));
        }

        [Test]
        [Category("UnitTest")]
        public void CueLabelsReturnsCorrectArray()
        {
            // arrange
            var cueList = new CueList();
            cueList.Add(new Cue(100, "Label1"));
            cueList.Add(new Cue(200, "Label2"));
            cueList.Add(new Cue(300, "Label3"));

            // act
            var labels = cueList.CueLabels;

            // assert
            Assert.That(labels, Has.Length.EqualTo(3));
            Assert.That(labels[0], Is.EqualTo("Label1"));
            Assert.That(labels[1], Is.EqualTo("Label2"));
            Assert.That(labels[2], Is.EqualTo("Label3"));
        }

        [Test]
        [Category("UnitTest")]
        public void CueConstructorHandlesNullLabel()
        {
            // arrange & act
            var cue = new Cue(500, null);

            // assert
            Assert.That(cue.Label, Is.EqualTo(string.Empty));
        }

        [Test]
        [Category("UnitTest")]
        public void CuePositionIsReadOnly()
        {
            // arrange
            var cue = new Cue(1000, "Label");

            // act & assert - Position is a read-only property
            Assert.That(cue.Position, Is.EqualTo(1000));
            // Verify property has no setter
            var prop = typeof(Cue).GetProperty("Position");
            Assert.That(prop.CanWrite, Is.False);
        }

        [Test]
        [Category("UnitTest")]
        public void CueLabelIsReadOnly()
        {
            // arrange
            var cue = new Cue(1000, "Label");

            // act & assert - Label is a read-only property
            Assert.That(cue.Label, Is.EqualTo("Label"));
            // Verify property has no setter
            var prop = typeof(Cue).GetProperty("Label");
            Assert.That(prop.CanWrite, Is.False);
        }

        [Test]
        [Category("UnitTest")]
        public void CanAccessCueByIndex()
        {
            // arrange
            var cueList = new CueList();
            var cue1 = new Cue(100, "Label1");
            var cue2 = new Cue(200, "Label2");
            cueList.Add(cue1);
            cueList.Add(cue2);

            // act & assert
            Assert.That(cueList[0].Position, Is.EqualTo(100));
            Assert.That(cueList[0].Label, Is.EqualTo("Label1"));
            Assert.That(cueList[1].Position, Is.EqualTo(200));
            Assert.That(cueList[1].Label, Is.EqualTo("Label2"));
        }

        [Test]
        [Category("UnitTest")]
        public void IndexerThrowsOutOfRangeWhenIndexTooLarge()
        {
            // arrange
            var cueList = new CueList();
            cueList.Add(new Cue(100, "Label1"));

            // act & assert
            Assert.That(() => { var x = cueList[5]; }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [Category("UnitTest")]
        public void IndexerThrowsOutOfRangeWhenIndexNegative()
        {
            // arrange
            var cueList = new CueList();
            cueList.Add(new Cue(100, "Label1"));

            // act & assert
            Assert.That(() => { var x = cueList[-1]; }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [Category("UnitTest")]
        public void MultipleCuesCanBeAddedAndRetrieved()
        {
            // arrange
            var cueList = new CueList();
            var expectedPositions = new[] { 1000, 2500, 5000, 7500, 10000 };
            var expectedLabels = new[] { "Intro", "Verse1", "Chorus", "Verse2", "Outro" };

            // act
            for (int i = 0; i < expectedPositions.Length; i++)
            {
                cueList.Add(new Cue(expectedPositions[i], expectedLabels[i]));
            }

            // assert
            Assert.That(cueList.Count, Is.EqualTo(expectedPositions.Length));
            var positions = cueList.CuePositions;
            var labels = cueList.CueLabels;
            
            Assert.That(positions, Is.EqualTo(expectedPositions));
            Assert.That(labels, Is.EqualTo(expectedLabels));
        }

        [Test]
        [Category("UnitTest")]
        public void CueListIndexerIsConsistentWithCuePositionsAndLabels()
        {
            // arrange
            var cueList = new CueList();
            cueList.Add(new Cue(100, "L1"));
            cueList.Add(new Cue(200, "L2"));
            cueList.Add(new Cue(300, "L3"));

            // act
            var positions = cueList.CuePositions;
            var labels = cueList.CueLabels;

            // assert
            for (int i = 0; i < cueList.Count; i++)
            {
                Assert.That(cueList[i].Position, Is.EqualTo(positions[i]));
                Assert.That(cueList[i].Label, Is.EqualTo(labels[i]));
            }
        }
    }
}
