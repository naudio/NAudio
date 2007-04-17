using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Daw
{
    class MeasuresBeatsTicks
    {
        private int measure;
        private int beat;
        private int tick;

        public int Measure
        {
            get { return measure; }
            set { measure = value; }
        }

        public int Beat
        {
            get { return beat; }
            set { beat = value; }
        }

        public int Tick
        {
            get { return tick; }
            set { tick = value; }
        }
    }
}
