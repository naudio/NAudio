using NAudio.Wave.Alsa;
using NAudio.Wave;
using System;

public abstract class AlsaPcm
{
    protected const uint PERIOD_QUANTITY = 8;
    protected const ulong PERIOD_SIZE = 1024;
    protected IntPtr Handle = default;
    protected IntPtr HwParams = default;
    protected IntPtr SwParams = default;
    protected bool isInitialized = false;
    public int Card { get; private set; }
    public uint Device { get; private set; }
    public string Id { get; private set; }
    public string Name { get; private set; }

    protected void GetHardwareParams()
    {
        AlsaInterop.PcmHwParamsMalloc(out HwParams);    
        AlsaInterop.PcmHwParamsAny(Handle, HwParams);
    }
    protected void SetHardwareParams()
    {
        int error;
        if ((error = AlsaInterop.PcmHwParams(Handle, HwParams)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new NotSupportedException(AlsaInterop.ErrorString(error));
        }
    }
    protected void GetSoftwareParams()
    {
        AlsaInterop.PcmSwParamsMalloc(out SwParams);
        AlsaInterop.PcmSwParamsCurrent(Handle, SwParams);
    }
    protected void SetInterleavedAccess()
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsTestAccess(Handle, HwParams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new Exception(AlsaInterop.ErrorString(error));
        }
        AlsaInterop.PcmHwParamsSetAccess(Handle, HwParams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED);
    }
    protected void SetSampleRate(uint sampleRate)
    {
        if (AlsaInterop.PcmHwParamsSetRate(Handle, HwParams, sampleRate, 0) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new NotSupportedException("Sample rate not supported.");
        }
    }
    protected void SetNumberOfChannels(uint numChannels)
    {
        if (AlsaInterop.PcmHwParamsSetChannels(Handle, HwParams, numChannels) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new NotSupportedException("Number of channels not supported.");
        }
    }
    protected void SetPeriods(ref uint periods, ref int dir)
    {
        if (AlsaInterop.PcmHwParamsSetPeriodsNear(Handle, HwParams, ref periods, ref dir) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new NotSupportedException("Periods not supported");
        }
    }
    protected void SetBufferSize(ref ulong buffer_size)
    {
        if (AlsaInterop.PcmHwParamsSetBufferSizeNear(Handle, HwParams, ref buffer_size) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new NotSupportedException("Buffer Size not supported");
        }
    }
}
