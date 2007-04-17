using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Daw
{
    /// <summary>
    /// Tempo map
    /// </summary>
    public class TempoMap
    {
        List<Tempo> tempos = new List<Tempo>();

        /// <summary>
        /// Create a new tempo map
        /// </summary>
        /// <param name="startTempo">Initial tempo</param>
        public TempoMap(double startTempo)
        {
            tempos.Add(new Tempo(TimeSpan.Zero,startTempo));
        }
    }

    /// <summary>
    /// Tempo
    /// </summary>
    public class Tempo
    {
        private TimeSpan beginTime;
        private double tempo;

        /// <summary>
        /// Start time
        /// </summary>
        public TimeSpan BeginTime
        {
            get { return beginTime; }
            set { beginTime = value; }
        }

        /// <summary>
        /// Tempo value in beats per minute
        /// </summary>
        public double Value
        {
            get { return tempo; }
            set { tempo = value; }
        }

        /// <summary>
        /// Create a new tempo
        /// </summary>
        /// <param name="beginTime">Start time</param>
        /// <param name="tempo">Tempo</param>
        public Tempo(TimeSpan beginTime, double tempo)
        {
            this.beginTime = beginTime;
            this.tempo = tempo;
        }
    }
}
