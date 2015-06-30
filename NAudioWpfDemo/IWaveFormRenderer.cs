using System;
using System.Linq;

namespace NAudioWpfDemo
{
    public interface IWaveFormRenderer
    {
        void AddValue(float maxValue, float minValue);
    }
}
