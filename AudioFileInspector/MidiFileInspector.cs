using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Midi;

namespace AudioFileInspector
{
    class MidiFileInspector : IAudioFileInspector
    {
        #region IAudioFileInspector Members

        public string FileExtension
        {
            get { return ".mid"; }
        }

        public string FileTypeDescription
        {
            get { return "Standard MIDI File"; }
        }

        public string Describe(string fileName)
        {
            MidiFile mf = new MidiFile(fileName, false);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}\r\n",
                mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);
            int beatsPerMeasure = FindBeatsPerMeasure(mf.Events[0]);
            for (int n = 0; n < mf.Tracks; n++)
            {
                foreach (MidiEvent midiEvent in mf.Events[n])
                {
                    if(!MidiEvent.IsNoteOff(midiEvent))
                    {
                        sb.AppendFormat("{0} {1}\r\n", ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, beatsPerMeasure), midiEvent);
                    }
                }
            }
            return sb.ToString();
        }

        private string ToMBT(long absoluteTime, int ticksPerBeat, int beatsPerMeasure)
        {
            long measure = (absoluteTime / (ticksPerBeat * beatsPerMeasure)) + 1;
            long beat = ((absoluteTime / ticksPerBeat) % beatsPerMeasure) + 1;
            long tick = absoluteTime % ticksPerBeat;
            return String.Format("{0}:{1}:{2}", measure, beat, tick);
        }

        private string ToMBT(MidiEvent midiEvent, int ticksPerBeat, List<TimeSignatureChange> timeSignatures)
        {
            TimeSignatureChange latestTimeSig = FindLatestTimeSig(midiEvent.AbsoluteTime,timeSignatures);
            long relativeTime = midiEvent.AbsoluteTime - latestTimeSig.AbsoluteTime;
            long measure = (relativeTime / (ticksPerBeat * latestTimeSig.BeatsPerMeasure)) + latestTimeSig.StartMeasureNumber;
            long beat = ((relativeTime / ticksPerBeat) % latestTimeSig.BeatsPerMeasure) + 1;
            long tick = relativeTime % ticksPerBeat;
            return String.Format("{0}:{1}:{2}", measure, beat, tick);
        }

        /// <summary>
        /// Find the number of beats per measure
        /// (for now assume just one TimeSignature per MIDI track)
        /// </summary>
        private int FindBeatsPerMeasure(IEnumerable<MidiEvent> midiEvents)
        {
            int beatsPerMeasure = 4;
            foreach (MidiEvent midiEvent in midiEvents)
            {
                TimeSignatureEvent tse = midiEvent as TimeSignatureEvent;
                if (tse != null)
                {
                    beatsPerMeasure = tse.Numerator;
                }
            }
            return beatsPerMeasure;
        }

        private TimeSignatureChange FindLatestTimeSig(long absoluteTime, List<TimeSignatureChange> timeSignatures)
        {
            TimeSignatureChange latestChange = null;
            foreach (TimeSignatureChange change in timeSignatures)
            {
                if (absoluteTime >= change.AbsoluteTime)
                    latestChange = change;
                else
                    break;
            }
            if (latestChange != null)
            {
                latestChange = new TimeSignatureChange(0, 4, 1);
            }
            return latestChange;
        }

        private List<TimeSignatureChange> FindTimeSignatures(List<MidiEvent> midiEvents)
        {
            long currentTime = -1;
            List<TimeSignatureChange> timeSignatureEvents = new List<TimeSignatureChange>();
            foreach (MidiEvent midiEvent in midiEvents)
            {
                TimeSignatureEvent tse = midiEvent as TimeSignatureEvent;
                if (tse != null)
                {
                    if (tse.AbsoluteTime <= currentTime)
                        throw new ArgumentException("Unsorted Time Signatures found");
                    // TODO: work out how to get the start measure
                    int startMeasure = 1;
                    timeSignatureEvents.Add(new TimeSignatureChange(tse.AbsoluteTime,tse.Numerator,startMeasure));
                    currentTime = tse.AbsoluteTime;
                }
            }
            return timeSignatureEvents;
        }

        class TimeSignatureChange
        {
            long absoluteTime;
            int beatsPerMeasure;
            int startMeasureNumber;

            public long AbsoluteTime
            {
                get { return absoluteTime; }
            }

            public int BeatsPerMeasure
            {
                get { return beatsPerMeasure; }
            }

            public int StartMeasureNumber
            {
                get { return startMeasureNumber; }
            }

            public TimeSignatureChange(long absoluteTime, int beatsPerMeasure, int startMeasureNumber)
            {
                this.absoluteTime = absoluteTime;
                this.beatsPerMeasure = beatsPerMeasure;
                this.startMeasureNumber = startMeasureNumber;
            }
        }

        #endregion
    }
}
