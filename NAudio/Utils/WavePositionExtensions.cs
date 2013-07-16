using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudio.Utils
{
    public static class WavePositionExtensions
    {
        public static TimeSpan GetPositionTimeSpan(this IWavePosition @this)
        {
            var pos = @this.GetPosition() / (@this.OutputWaveFormat.Channels * @this.OutputWaveFormat.BitsPerSample / 8);
            return TimeSpan.FromMilliseconds(pos * 1000.0 / @this.OutputWaveFormat.SampleRate);
        }
    }
}
