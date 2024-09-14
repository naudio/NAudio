using NAudio.Wave.Alsa;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract class AlsaPcm
{
    protected static readonly uint[] rates = 
    {
        8000,
        11025,
        16000,
        22050,
        32000,
        44100,
        48000,
        64000,
        64000,
        88200,
        96000,
        176400,
        192000
    };
    protected const uint PERIOD_QUANTITY = 8;
    protected const ulong PERIOD_SIZE = 1024;
    protected IntPtr Handle = default;
    protected IntPtr HwParams = default;
    protected IntPtr SwParams = default;
    protected int BufferNum;
    protected byte[] WaveBuffer;
    protected byte[][] Buffers;
    protected bool isInitialized = false;
    protected bool isDisposed = false;
    public int Card { get; private set; }
    public uint Device { get; private set; }
    public string Id { get; private set; }
    public string Name { get; private set; }
    public int NumberOfBuffers { get; set; } = 2;
    public bool Async { get; private set; }

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
            throw new AlsaException(error);
        }
    }
    protected void GetSoftwareParams()
    {
        AlsaInterop.PcmSwParamsMalloc(out SwParams);
        AlsaInterop.PcmSwParamsCurrent(Handle, SwParams);
    }
    protected void SetSoftwareParams()
    {
        int error;
        if ((error = AlsaInterop.PcmSwParams(Handle, SwParams)) < 0)
        {
            AlsaInterop.PcmSwParamsFree(SwParams);
            throw new AlsaException(error);
        }
    }
    protected void SetInterleavedAccess()
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsTestAccess(Handle, HwParams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new AlsaException(error);
        }
        AlsaInterop.PcmHwParamsSetAccess(Handle, HwParams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED);
    }
    protected void SetSampleRate(uint sampleRate)
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsSetRate(Handle, HwParams, sampleRate, 0)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new AlsaException(error);
        }
    }
    protected void SetNumberOfChannels(uint numChannels)
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsSetChannels(Handle, HwParams, numChannels)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new AlsaException(error);
        }
    }
    protected void SetPeriods(ref uint periods, ref int dir)
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsSetPeriodsNear(Handle, HwParams, ref periods, ref dir)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new AlsaException(error);
        }
    }
    protected void SetBufferSize(ref ulong buffer_size)
    {
        int error;
        if ((error = AlsaInterop.PcmHwParamsSetBufferSizeNear(Handle, HwParams, ref buffer_size)) != 0)
        {
            AlsaInterop.PcmHwParamsFree(HwParams);
            throw new AlsaException(error);
        }
    }
    protected static ulong GetSampleSize(WaveFormat format)
    {
        return (ulong)(format.BitsPerSample / 8 * format.Channels);
    }
    private static int[] GetValidChannelValues(IntPtr Handle, IntPtr HwParams)
    {
        AlsaInterop.PcmHwParamsGetChannelsMin(HwParams, out uint min);
        AlsaInterop.PcmHwParamsGetChannelsMax(HwParams, out uint max);
        int[] result = new int[0];
        for (uint i = min; i <= max; i++)
        {
            if (AlsaInterop.PcmHwParamsTestChannels(Handle, HwParams, i) == 0)
            {
                int[] newresult = new int[result.Length + 1];
                Array.Copy(result, newresult, result.Length);
                newresult[result.Length] = (int)i;
                result = newresult;
            }
        }
        return result;
    }
    private static int[] GetValidSampleRates(IntPtr Handle, IntPtr HwParams)
    {
        int[] result = new int[0];
        for (uint i = 0; i < rates.Length; i++)
        {
            var rate = rates[i];
            if (AlsaInterop.PcmHwParamsTestRate(Handle, HwParams, rate, 0) == 0)
            {
                int[] newresult = new int[result.Length + 1];
                Array.Copy(result, newresult, result.Length);
                newresult[result.Length] = (int)rates[i];
                result = newresult;
            }
        }
        return result;
    }
    private static PCMFormat[] GetValidFormats(IntPtr Handle, IntPtr HwParams)
    {
        PCMFormat[] result = new PCMFormat[0];
        for (PCMFormat i = 0; i < PCMFormat.SND_PCM_FORMAT_LAST; i++)
        {
            if (AlsaInterop.PcmHwParamsTestFormat(Handle, HwParams, i) == 0)
            {
                PCMFormat[] newresult = new PCMFormat[result.Length + 1];
                Array.Copy(result, newresult, result.Length);
                newresult[result.Length] = i;
                result = newresult;
            }
        }
        return result;
    }
    private static WaveFormat[] GetValidWaveFormats(IntPtr Handle, IntPtr HwParams)
    {
        var valid_rates = GetValidSampleRates(Handle, HwParams);
        var valid_channels = GetValidChannelValues(Handle, HwParams);
        var valid_formats = GetValidFormats(Handle, HwParams);
        List<WaveFormat> valid_waveformats = new List<WaveFormat>();
        for (int i = 0; i < valid_formats.Length; i++)
        {
            switch (valid_formats[i])
            {
                case PCMFormat.SND_PCM_FORMAT_FLOAT_LE:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(WaveFormat.CreateIeeeFloatWaveFormat(valid_rates[j], valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_U8:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(new WaveFormat(valid_rates[j], 8, valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_S16_LE:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(new WaveFormat(valid_rates[j], 16, valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_S24_3LE:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(new WaveFormat(valid_rates[j], 24, valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_S32_LE:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(new WaveFormat(valid_rates[j], 32, valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_A_LAW:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(WaveFormat.CreateALawFormat(valid_rates[j], valid_channels[k]));
                        }
                    }
                    break;
                case PCMFormat.SND_PCM_FORMAT_MU_LAW:
                    for (int j = 0; j < valid_rates.Length; j++)
                    {
                        for (int k = 0; k < valid_channels.Length; k++)
                        {
                            valid_waveformats.Add(WaveFormat.CreateMuLawFormat(valid_rates[j], valid_channels[k]));
                        }
                    }
                    break;

            }
        }
        return valid_waveformats.ToArray();
    }
    public WaveFormat[] GetValidWaveFormats()
    {
        return GetValidWaveFormats(Handle, HwParams);
    }
    public WaveFormat GetCurrentWaveFormat()
    {
        WaveFormat result = null;
        var formats = GetValidWaveFormats();
        if (formats.Length == 1)
        {
            result = formats[0];
        }
        return result;
    }
    public static bool TestWaveFormat(WaveFormat waveFormat, IntPtr Handle)
    {
        AlsaInterop.PcmHwParamsMalloc(out IntPtr hwparams);
        AlsaInterop.PcmHwParamsAny(Handle, hwparams);
        var result = GetValidWaveFormats(Handle, hwparams).Contains(waveFormat);
        AlsaInterop.PcmHwParamsFree(hwparams);
        return result;
    }
    public bool TestWaveFormat(WaveFormat waveFormat)
    {
        return TestWaveFormat(waveFormat, Handle);
    }
    protected void SwapBuffers()
    {
        BufferNum = ++BufferNum % NumberOfBuffers;
        WaveBuffer = Buffers[BufferNum];
    }
    protected void InitBuffers(bool async)
    {
        int error;
        Async = async;
        ulong buffer_size = PERIOD_SIZE * PERIOD_QUANTITY;
        if (!async)
        {
            Buffers = new byte[NumberOfBuffers][];
            for (int i = 0; i < NumberOfBuffers; i++)
            {
                Buffers[i] = new byte[buffer_size];    
            }
            WaveBuffer = Buffers[BufferNum];
        }
        else
        {
            WaveBuffer = new byte[buffer_size];
        }
    }
}
