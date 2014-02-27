using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.Midi
{
    /// <summary>
    /// A helper class to manage collection of MIDI events
    /// It has the ability to organise them in tracks
    /// </summary>
    public class MidiEventCollection : IEnumerable<IList<MidiEvent>>
    {
        int midiFileType;
        List<IList<MidiEvent>> trackEvents;
        int deltaTicksPerQuarterNote;
        long startAbsoluteTime;

        /// <summary>
        /// Creates a new Midi Event collection
        /// </summary>
        /// <param name="midiFileType">Initial file type</param>
        /// <param name="deltaTicksPerQuarterNote">Delta Ticks Per Quarter Note</param>
        public MidiEventCollection(int midiFileType, int deltaTicksPerQuarterNote)
        {
            this.midiFileType = midiFileType;
            this.deltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
            this.startAbsoluteTime = 0;
            trackEvents = new List<IList<MidiEvent>>();
        }

        /// <summary>
        /// The number of tracks
        /// </summary>
        public int Tracks
        {
            get
            {
                return trackEvents.Count;
            }
        }

        /// <summary>
        /// The absolute time that should be considered as time zero
        /// Not directly used here, but useful for timeshifting applications
        /// </summary>
        public long StartAbsoluteTime
        {
            get
            {
                return startAbsoluteTime;
            }
            set
            {
                startAbsoluteTime = value;
            }
        }

        /// <summary>
        /// The number of ticks per quarter note
        /// </summary>
        public int DeltaTicksPerQuarterNote
        {
            get { return deltaTicksPerQuarterNote; }
        }

        /// <summary>
        /// Gets events on a specified track
        /// </summary>
        /// <param name="trackNumber">Track number</param>
        /// <returns>The list of events</returns>
        public IList<MidiEvent> GetTrackEvents(int trackNumber)
        {
            return trackEvents[trackNumber];
        }

        /// <summary>
        /// Gets events on a specific track
        /// </summary>
        /// <param name="trackNumber">Track number</param>
        /// <returns>The list of events</returns>
        public IList<MidiEvent> this[int trackNumber]
        {
            get { return trackEvents[trackNumber]; }
        }

        /// <summary>
        /// Adds a new track
        /// </summary>
        /// <returns>The new track event list</returns>
        public IList<MidiEvent> AddTrack()
        {
            return AddTrack(null);
        }

        /// <summary>
        /// Adds a new track
        /// </summary>
        /// <param name="initialEvents">Initial events to add to the new track</param>
        /// <returns>The new track event list</returns>
        public IList<MidiEvent> AddTrack(IList<MidiEvent> initialEvents)
        {
            List<MidiEvent> events = new List<MidiEvent>();
            if (initialEvents != null)
            {
                events.AddRange(initialEvents);
            }
            trackEvents.Add(events);
            return events;
        }

        /// <summary>
        /// Removes a track
        /// </summary>
        /// <param name="track">Track number to remove</param>
        public void RemoveTrack(int track)
        {
            trackEvents.RemoveAt(track);
        }

        /// <summary>
        /// Clears all events
        /// </summary>
        public void Clear()
        {
            trackEvents.Clear();
        }

        /// <summary>
        /// The MIDI file type
        /// </summary>
        public int MidiFileType
        {
            get
            {
                return midiFileType;
            }
            set
            {
                if (midiFileType != value)
                {
                    // set MIDI file type before calling flatten or explode functions
                    midiFileType = value;
                                        
                    if (value == 0)
                    {
                        FlattenToOneTrack();
                    }
                    else
                    {
                        ExplodeToManyTracks();
                    }
                }
            }
        }

        /// <summary>
        /// Adds an event to the appropriate track depending on file type
        /// </summary>
        /// <param name="midiEvent">The event to be added</param>
        /// <param name="originalTrack">The original (or desired) track number</param>
        /// <remarks>When adding events in type 0 mode, the originalTrack parameter
        /// is ignored. If in type 1 mode, it will use the original track number to
        /// store the new events. If the original track was 0 and this is a channel based
        /// event, it will create new tracks if necessary and put it on the track corresponding
        /// to its channel number</remarks>
        public void AddEvent(MidiEvent midiEvent, int originalTrack)
        {
            if (midiFileType == 0)
            {
                EnsureTracks(1);
                trackEvents[0].Add(midiEvent);
            }
            else
            {
                if(originalTrack == 0)
                {
                    // if its a channel based event, lets move it off to
                    // a channel track of its own
                    switch (midiEvent.CommandCode)
                    {
                        case MidiCommandCode.NoteOff:
                        case MidiCommandCode.NoteOn:
                        case MidiCommandCode.KeyAfterTouch:
                        case MidiCommandCode.ControlChange:
                        case MidiCommandCode.PatchChange:
                        case MidiCommandCode.ChannelAfterTouch:
                        case MidiCommandCode.PitchWheelChange:
                            EnsureTracks(midiEvent.Channel + 1);
                            trackEvents[midiEvent.Channel].Add(midiEvent);
                            break;
                        default:
                            EnsureTracks(1);
                            trackEvents[0].Add(midiEvent);
                            break;
                    }

                }
                else
                {
                    // put it on the track it was originally on
                    EnsureTracks(originalTrack + 1);
                    trackEvents[originalTrack].Add(midiEvent);
                }
            }
        }


        private void EnsureTracks(int count)
        {
            for (int n = trackEvents.Count; n < count; n++)
            {
                trackEvents.Add(new List<MidiEvent>());
            }
        }

        private void ExplodeToManyTracks()
        {
            IList<MidiEvent> originalList = trackEvents[0];
            Clear();
            foreach (MidiEvent midiEvent in originalList)
            {
                AddEvent(midiEvent, 0);
            }
            PrepareForExport();
        }

        private void FlattenToOneTrack()
        {
            bool eventsAdded = false;
            for (int track = 1; track < trackEvents.Count; track++)
            {
                foreach (MidiEvent midiEvent in trackEvents[track])
                {
                    if (!MidiEvent.IsEndTrack(midiEvent))
                    {
                        trackEvents[0].Add(midiEvent);
                        eventsAdded = true;
                    }
                }
            }
            for (int track = trackEvents.Count - 1; track > 0; track--)
            {
                RemoveTrack(track);
            }
            if (eventsAdded)
            {
                PrepareForExport();
            }
        }

        /// <summary>
        /// Sorts, removes empty tracks and adds end track markers
        /// </summary>
        public void PrepareForExport()
        {
            var comparer = new MidiEventComparer();
            // 1. sort each track
            foreach (List<MidiEvent> list in trackEvents)
            {
                MergeSort.Sort(list, comparer);

                // 2. remove all End track events except one at the very end
                int index = 0;
                while (index < list.Count - 1)
                {
                    if(MidiEvent.IsEndTrack(list[index]))
                    {
                        list.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            int track = 0;
            // 3. remove empty tracks and add missing
            while (track < trackEvents.Count)
            {
                IList<MidiEvent> list = trackEvents[track];
                if (list.Count == 0)
                {
                    RemoveTrack(track);
                }
                else
                {
                    if(list.Count == 1 && MidiEvent.IsEndTrack(list[0]))
                    {
                        RemoveTrack(track);
                    }
                    else
                    {
                        if(!MidiEvent.IsEndTrack(list[list.Count-1]))
                        {
                            list.Add(new MetaEvent(MetaEventType.EndTrack, 0, list[list.Count - 1].AbsoluteTime));
                        }
                        track++;
                    }
                }
            }
        }

        /// <summary>
        /// Gets an enumerator for the lists of track events
        /// </summary>
        public IEnumerator<IList<MidiEvent>> GetEnumerator()
        {
            return trackEvents.GetEnumerator();
            
        }

        /// <summary>
        /// Gets an enumerator for the lists of track events
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return trackEvents.GetEnumerator();
        }
    }
}
