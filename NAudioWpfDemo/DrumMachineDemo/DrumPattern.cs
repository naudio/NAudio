using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudioWpfDemo.DrumMachineDemo
{
    public class DrumPattern
    {
        private readonly byte[,] hits;
        private readonly List<string> noteNames;
        
        public DrumPattern(IEnumerable<string> notes, int steps)
        {
            noteNames = new List<string>(notes);
            Steps = steps;
            hits = new byte[Notes, steps];
        }

        public int Steps { get; }
        
        public int Notes => noteNames.Count;

        public byte this[int note, int step]
        {
            get => hits[note, step];
            set
            {
                if (hits[note, step] != value)
                {
                    hits[note, step] = value;
                    PatternChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler PatternChanged;

        public IList<string> NoteNames => noteNames.AsReadOnly();
    }
}
