using System;
using NAudio.Wave.Alsa;

[Serializable]
public class AlsaException : Exception
{
    public AlsaException(int error) : base(AlsaInterop.ErrorString(error))
    {
    }
    public AlsaException(string message, int error) : base(string.Format("{0}: {1}", message, AlsaInterop.ErrorString(error)))
    {
    }
}