using System;
using System.Linq;

namespace NAudioAvaloniaDemo
{
    public interface IWaveFormRenderer
    {
        void AddValue(float maxValue, float minValue);
    }
}
