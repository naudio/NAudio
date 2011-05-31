using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NAudioWpfDemo.DrumMachineDemo
{
    public class DrumPattern
    {
        private byte[,] hits;

        public DrumPattern(int notes, int steps)
        {
            this.Notes = notes;
            this.Steps = steps;
            hits = new byte[notes, steps];
        }

        public int Steps { get; private set; }
        public int Notes { get; private set; }

        public byte this[int note, int step]
        {
            get { return hits[note, step]; }
            set
            {
                if (hits[note, step] != value)
                {
                    hits[note, step] = value;
                    if (PatternChanged != null)
                    {
                        PatternChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler PatternChanged;
    }

}
