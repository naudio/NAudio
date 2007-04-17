using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAudio.Midi;
using NAudio.Utils;
using MarkHeath.MidiUtils.Properties;

namespace MarkHeath.MidiUtils
{
    class MidiFileSplitter
    {
        public event EventHandler<ProgressEventArgs> Progress;
        Settings settings = Settings.Default;

        public int SplitMidiFile(string filename)
        {
            int exported = 0;
            try
            {
                LogInformation("Processing file: {0}", filename);
                MidiFile midiFile = new MidiFile(filename,!settings.AllowOrphanedNoteEvents);
                List<TextEvent> markers = FindMarkers(midiFile.Events[0]);
                if (settings.LyricsAsMarkers)
                {
                    for (int track = 1; track < midiFile.Tracks; track++)
                    {
                        markers.AddRange(FindMarkers(midiFile.Events[track]));
                    }
                }
                if (markers.Count == 0)
                {
                    LogWarning("No markers found in file {0}", Path.GetFileName(filename));
                    return 0;
                }
                /*if (markers.Count == 1)
                {
                    LogWarning("Only one marker found in file {0}", Path.GetFileName(filename));
                    return 0;
                }*/

                if(markers[0].AbsoluteTime != 0)
                {
                    LogWarning("Inserting a new START marker");
                    markers.Insert(0,new TextEvent("START",MetaEventType.Marker,0));
                }

                for (int n = 0; n < markers.Count; n++)
                {
                    try
                    {
                        ExportAtMarker(midiFile, markers[n].Text, filename,
                            markers[n].AbsoluteTime, (n < markers.Count - 1) ? markers[n + 1].AbsoluteTime : Int64.MaxValue);
                        exported++;
                    }
                    catch (IOException ioe)
                    {
                        LogError(ioe.Message);
                    }
                }
            }
            catch (FormatException fe)
            {
                LogError("{0} was not recognised as a valid MIDI file", Path.GetFileName(filename));
                LogError(fe.Message);
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message);
            }
            catch (Exception e)
            {                
                LogError("Unexpected Error: {0}", e.Message);
            }
            return exported;
        }

        private void LogInformation(string message, params object[] args)
        {
            OnProgress(this,new ProgressEventArgs(ProgressMessageType.Information,
                message,args));
        }

        private void LogWarning(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Warning,
                message, args));
        }

        private void LogError(string message, params object[] args)
        {
            OnProgress(this, new ProgressEventArgs(ProgressMessageType.Error,
                message, args));
        }

        protected void OnProgress(object sender, ProgressEventArgs args)
        {
            if (Progress != null)
            {
                Progress(sender, args);
            }
        }

        private List<TextEvent> FindMarkers(IList<MidiEvent> events)
        {
            List<TextEvent> markers = new List<TextEvent>();
            foreach (MidiEvent midiEvent in events)
            {
                TextEvent textEvent = midiEvent as TextEvent;
                if (textEvent != null)
                {
                    if (textEvent.MetaEventType == MetaEventType.Marker)
                    {
                        markers.Add(textEvent);
                    }
                    else if (settings.TextEventMarkers && textEvent.MetaEventType == MetaEventType.TextEvent)
                    {
                        markers.Add(textEvent);
                    }
                    else if (settings.LyricsAsMarkers && textEvent.MetaEventType == MetaEventType.Lyric)
                    {
                        markers.Add(textEvent);
                    }
                }
            }
            return markers;
        }

        private void CreateTrackZeroEvents(List<MidiEvent> trackZeroEvents, MidiFile midiFile, long startAbsoluteTime, long endAbsoluteTime, bool includeAllTrackEvents)
        {
            MetaEvent tempoEvent = null;
            MetaEvent keySignatureEvent = null;
            MetaEvent timeSignatureEvent = null;
            bool gotAStartTempo = false;
            bool gotAStartKeySig = false;
            bool gotAStartTimeSig = false;

            for (int track = 0; track < ((includeAllTrackEvents) ? midiFile.Tracks : 1); track++)
            {
                foreach (MidiEvent midiEvent in midiFile.Events[track])
                {
                    if ((midiEvent.AbsoluteTime >= startAbsoluteTime) && (midiEvent.AbsoluteTime < endAbsoluteTime))
                    {
                        bool exclude = false;
                        MetaEvent metaEvent = midiEvent as MetaEvent;
                        if (metaEvent != null)
                        {
                            if (metaEvent.MetaEventType == MetaEventType.EndTrack)
                            {
                                // we'll add our own
                                exclude = true;
                            }
                            if (metaEvent.AbsoluteTime == startAbsoluteTime)
                            {
                                switch (metaEvent.MetaEventType)
                                {
                                    case MetaEventType.SetTempo:
                                        gotAStartTempo = true;
                                        break;
                                    case MetaEventType.KeySignature:
                                        gotAStartKeySig = true;
                                        break;
                                    case MetaEventType.TimeSignature:
                                        gotAStartTimeSig = true;
                                        break;
                                    case MetaEventType.Marker:
                                        // already done this elsewhere
                                        exclude = true;
                                        break;
                                    case MetaEventType.TextEvent:
                                        // exclude if text events as markers is on
                                        exclude = settings.TextEventMarkers;
                                        break;
                                }
                            }
                        }
                        else
                        {
                            exclude = !includeAllTrackEvents;
                        }
                        if (!exclude)
                        {
                            AddMidiEvent(midiEvent, trackZeroEvents, endAbsoluteTime);
                        }

                    }
                    else if (midiEvent.AbsoluteTime < startAbsoluteTime)
                    {
                        // TODO: perhaps look out for a patch change too
                        MetaEvent metaEvent = midiEvent as MetaEvent;
                        if (metaEvent != null)
                        {
                            switch (metaEvent.MetaEventType)
                            {
                                case MetaEventType.TextEvent:
                                case MetaEventType.Copyright:
                                case MetaEventType.SequenceTrackName:
                                    //TextEvent te = (TextEvent)metaEvent;
                                    //trackZeroEvents.Add(new TextEvent(te.Text, metaEvent.MetaEventType, startAbsoluteTime));
                                    break;
                                case MetaEventType.KeySignature:
                                    KeySignatureEvent kse = (KeySignatureEvent)metaEvent;
                                    keySignatureEvent = new KeySignatureEvent(kse.SharpsFlats, kse.MajorMinor, startAbsoluteTime);
                                    break;
                                case MetaEventType.SetTempo:
                                    tempoEvent = new TempoEvent(((TempoEvent)metaEvent).MicrosecondsPerQuarterNote, startAbsoluteTime); ;
                                    break;
                                case MetaEventType.TimeSignature:
                                    TimeSignatureEvent tse = (TimeSignatureEvent)metaEvent;
                                    timeSignatureEvent = new TimeSignatureEvent(tse.Numerator, tse.Denominator, tse.TicksInMetronomeClick, tse.No32ndNotesInQuarterNote, startAbsoluteTime);
                                    break;
                                case MetaEventType.TrackSequenceNumber:
                                    // TODO: needed?
                                    break;
                                case MetaEventType.TrackInstrumentName:
                                case MetaEventType.Lyric:
                                case MetaEventType.CuePoint:
                                case MetaEventType.Marker:
                                case MetaEventType.SequencerSpecific:
                                case MetaEventType.DeviceName:
                                case MetaEventType.ProgramName:
                                case MetaEventType.SmpteOffset:
                                case MetaEventType.EndTrack:
                                    // ignore these
                                    break;
                                default:
                                    //System.Diagnostics.Debug.Assert(false, String.Format("Unexpected meta event type {0}", metaEvent));
                                    break;
                            }
                        }
                    }
                }
            }
            if ((tempoEvent != null) && (!gotAStartTempo))
                trackZeroEvents.Add(tempoEvent);
            if ((keySignatureEvent != null) && (!gotAStartKeySig))
                trackZeroEvents.Add(keySignatureEvent);
            if ((timeSignatureEvent != null) && (!gotAStartTimeSig))
                trackZeroEvents.Add(timeSignatureEvent);

            // add an end track marker
            trackZeroEvents.Sort(new MidiEventComparer());
            trackZeroEvents.Add(new MetaEvent(MetaEventType.EndTrack,0,trackZeroEvents[trackZeroEvents.Count-1].AbsoluteTime));
        }

        private void CreateTrackEvents(List<MidiEvent> outputList, IList<MidiEvent> inputList, long startAbsoluteTime, long endAbsoluteTime, bool allowMetaEvents)
        {
            foreach (MidiEvent midiEvent in inputList)
            {
                if ((midiEvent.AbsoluteTime >= startAbsoluteTime) && (midiEvent.AbsoluteTime < endAbsoluteTime))
                {
                    bool exclude = false;
                    MetaEvent metaEvent = midiEvent as MetaEvent;
                    if (metaEvent != null)
                    {
                        if (allowMetaEvents)
                        {
                            if (metaEvent.MetaEventType == MetaEventType.EndTrack)
                            {
                                exclude = true;
                            }
                            else if (metaEvent.MetaEventType == MetaEventType.SequenceTrackName)
                            {
                                exclude = true;
                            }
                        }
                        else
                        {
                            exclude = true;
                        }
                    }

                    if (!exclude)
                    {
                        AddMidiEvent(midiEvent, outputList, endAbsoluteTime);
                    }
                }
            }

            outputList.Sort(new MidiEventComparer());            
            outputList.Add(new MetaEvent(MetaEventType.EndTrack, 0, outputList[outputList.Count - 1].AbsoluteTime));
        }

        private void ExportAtMarker(MidiFile midiFile, string markerName, string midiFilename, long startAbsoluteTime, long endAbsoluteTime)
        {
            string exportFileName = CreateFileName(markerName, midiFilename);
            LogInformation("Exporting Marker {0} to {1}", markerName, exportFileName);

            // 1. Construct a list of meta-events for track zero
            List<MidiEvent> trackZeroEvents = new List<MidiEvent>();
            trackZeroEvents.Add(new TextEvent(markerName, MetaEventType.SequenceTrackName, startAbsoluteTime));
            CreateTrackZeroEvents(trackZeroEvents, midiFile, startAbsoluteTime, endAbsoluteTime, settings.MidiFileType == 0);
            MidiEventCollection exportEvents = new MidiEventCollection(settings.MidiFileType, midiFile.DeltaTicksPerQuarterNote);
            exportEvents.AddTrack(trackZeroEvents);
            
            if (settings.MidiFileType == 1)
            {
                if (midiFile.Tracks == 1)
                {
                    // this is a special case - we got a type 0 or a strange type 1 with just 1 track
                    // turn it into a type 1 with notes on the second track
                    List<MidiEvent> trackOneEvents = new List<MidiEvent>();
                    trackOneEvents.Add(new TextEvent(markerName, MetaEventType.SequenceTrackName, startAbsoluteTime));
                    CreateTrackEvents(trackOneEvents, midiFile.Events[0], startAbsoluteTime, endAbsoluteTime, false);
                    exportEvents.AddTrack(trackOneEvents);
                }

                for (int track = 1; track < midiFile.Tracks; track++)
                {
                    List<MidiEvent> trackEvents = new List<MidiEvent>();
                    trackEvents.Add(new TextEvent(markerName, MetaEventType.SequenceTrackName, startAbsoluteTime));
                    CreateTrackEvents(trackEvents, midiFile.Events[track], startAbsoluteTime, endAbsoluteTime, true);
                    exportEvents.AddTrack(trackEvents);
                }
            }

            
            
            bool gotNotes = false;
            foreach(IList<MidiEvent> trackEvents in exportEvents)
            {            
                foreach(MidiEvent trackEvent in trackEvents)
                {
                    if (trackEvent.CommandCode == MidiCommandCode.NoteOn)
                    {
                        gotNotes = true;
                        break;
                    }
                }
            }

            if (!gotNotes)
            {
                LogInformation("No note events found for this marker {0}", markerName);
                return;
            }

            exportEvents.StartAbsoluteTime = startAbsoluteTime; 
            MidiFile.Export(exportFileName, exportEvents);
        }

        private void AddMidiEvent(MidiEvent midiEvent, List<MidiEvent> eventList, long endMarkerTime)
        {
            bool exclude = false;
            NoteEvent noteEvent = midiEvent as NoteEvent;
            if (noteEvent != null)
            {
                if (noteEvent.CommandCode == MidiCommandCode.NoteOff)
                {
                    exclude = true;
                }
                else if (noteEvent.CommandCode == MidiCommandCode.NoteOn)
                {
                    if (noteEvent.Velocity == 0)
                    {
                        // it is effectively a note off
                        exclude = true;
                    }
                    else
                    {
                        NoteOnEvent noteOnEvent = (NoteOnEvent)noteEvent;
                        if (noteOnEvent.OffEvent != null)
                        {
                            if (settings.NoteDuration == NoteDurationSettings.ConstantLength)
                            {
                                noteOnEvent.NoteLength = settings.FixedNoteLength;
                            }
                            else if (settings.NoteDuration == NoteDurationSettings.Truncate)
                            {
                                if (noteOnEvent.OffEvent.AbsoluteTime >= endMarkerTime)
                                {
                                    if (noteOnEvent.AbsoluteTime == endMarkerTime - 1)
                                    {
                                        LogError("Could not truncate note - too close to end marker: {0}", noteOnEvent);
                                        noteOnEvent.OffEvent.AbsoluteTime = endMarkerTime;
                                    }
                                    else
                                    {
                                        noteOnEvent.OffEvent.AbsoluteTime = endMarkerTime - 1;
                                    }
                                }
                            }

                            if (settings.ModifyChannel)
                            {
                                if (midiEvent.CommandCode != MidiCommandCode.MetaEvent)
                                {
                                    noteOnEvent.OffEvent.Channel = settings.FixedChannel;
                                }
                            }
                            eventList.Add(noteOnEvent.OffEvent);
                        }
                        else
                        {
                            // just let it through. user has chosen to allow
                            // orphaned note events if we are here
                            
                        }
                    }
                }
            }
            if (!exclude)
            {
                if (settings.ModifyChannel)
                {
                    if (midiEvent.CommandCode != MidiCommandCode.MetaEvent)
                    {
                        midiEvent.Channel = settings.FixedChannel;
                    }
                }

                eventList.Add(midiEvent);
            }
        }


        private string CreateFileName(string markerName, string midiFilename)
        {
            string path = null;
            if (settings.OutputFolder == OutputFolderSettings.SameFolder)
            {
                path = Path.GetDirectoryName(midiFilename);
            }
            else if (settings.OutputFolder == OutputFolderSettings.SubFolder)
            {
                path = Path.Combine(Path.GetDirectoryName(midiFilename), Path.GetFileNameWithoutExtension(midiFilename));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    LogWarning("Created new folder: {0}", path);
                }
            }
            else if (settings.OutputFolder == OutputFolderSettings.CustomFolder)
            {
                path = settings.CustomFolder;
            }
            else if (settings.OutputFolder == OutputFolderSettings.CustomSubFolder)
            {
                path = Path.Combine(settings.CustomFolder, Path.GetFileNameWithoutExtension(midiFilename));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    LogWarning("Created new folder: {0}", path);
                }
            }
            //Path.InvalidPathChars
            if (markerName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    markerName = markerName.Replace(c, ' ');
                }
            }
            
            markerName = markerName.Replace('.', ' ');
            markerName = markerName.TrimEnd();
            if (markerName.Length == 0)
                markerName = "NO NAME";

            string filename = null;
            int attempt = 0;
            const int maxAttempts = 100;
            while (attempt < maxAttempts)
            {
                filename = Path.Combine(path, String.Format("{0}{1}.mid",
                    markerName, attempt > 0 ? String.Format(" ({0})", attempt) : "")); ;
                if (File.Exists(filename))
                {
                    if (settings.UniqueFilename)
                    {
                        attempt++;
                    }
                    else
                    {
                        throw new IOException(String.Format("File already exists {0}", filename));
                    }
                }
                else
                {
                    break;
                }
            }
            if (attempt >= maxAttempts)
            {
                throw new IOException(String.Format("Filed to generate a unique filename for marker {0}", markerName));
            }
            return filename;
        }
    }
}
