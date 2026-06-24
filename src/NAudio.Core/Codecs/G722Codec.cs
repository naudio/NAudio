using System;

namespace NAudio.Codecs
{
    /// <summary>
    /// SpanDSP - a series of DSP components for telephony
    /// 
    /// g722_decode.c - The ITU G.722 codec, decode part.
    /// 
    /// Written by Steve Underwood &lt;steveu@coppice.org&gt;
    /// 
    /// Copyright (C) 2005 Steve Underwood
    /// Ported to C# by Mark Heath 2011
    /// 
    /// Despite my general liking of the GPL, I place my own contributions 
    /// to this code in the public domain for the benefit of all mankind -
    /// even the slimy ones who might try to proprietize my work and use it
    /// to my detriment.
    ///  
    /// Based in part on a single channel G.722 codec which is:
    /// Copyright (c) CMU 1993
    /// Computer Science, Speech Group
    /// Chengxiang Lu and Alex Hauptmann
    /// </summary>
    public class G722Codec
    {
        /// <summary>
        /// hard limits to 16 bit samples
        /// </summary>
        static short Saturate(int amp)
        {
            short amp16;

            // Hopefully this is optimised for the common case - not clipping
            amp16 = (short)amp;
            if (amp == amp16)
                return amp16;
            if (amp > Int16.MaxValue)
                return Int16.MaxValue;
            return Int16.MinValue;
        }

        static void Block4(G722CodecState s, int band, int d)
        {
            int wd1;
            int wd2;
            int wd3;
            int i;

            // Block 4, RECONS
            s.Band[band].d[0] = d;
            s.Band[band].r[0] = Saturate(s.Band[band].s + d);

            // Block 4, PARREC
            s.Band[band].p[0] = Saturate(s.Band[band].sz + d);

            // Block 4, UPPOL2
            for (i = 0; i < 3; i++)
                s.Band[band].sg[i] = s.Band[band].p[i] >> 15;
            wd1 = Saturate(s.Band[band].a[1] << 2);

            wd2 = (s.Band[band].sg[0] == s.Band[band].sg[1]) ? -wd1 : wd1;
            if (wd2 > 32767)
                wd2 = 32767;
            wd3 = (s.Band[band].sg[0] == s.Band[band].sg[2]) ? 128 : -128;
            wd3 += (wd2 >> 7);
            wd3 += (s.Band[band].a[2] * 32512) >> 15;
            if (wd3 > 12288)
                wd3 = 12288;
            else if (wd3 < -12288)
                wd3 = -12288;
            s.Band[band].ap[2] = wd3;

            // Block 4, UPPOL1
            s.Band[band].sg[0] = s.Band[band].p[0] >> 15;
            s.Band[band].sg[1] = s.Band[band].p[1] >> 15;
            wd1 = (s.Band[band].sg[0] == s.Band[band].sg[1]) ? 192 : -192;
            wd2 = (s.Band[band].a[1] * 32640) >> 15;

            s.Band[band].ap[1] = Saturate(wd1 + wd2);
            wd3 = Saturate(15360 - s.Band[band].ap[2]);
            if (s.Band[band].ap[1] > wd3)
                s.Band[band].ap[1] = wd3;
            else if (s.Band[band].ap[1] < -wd3)
                s.Band[band].ap[1] = -wd3;

            // Block 4, UPZERO
            wd1 = (d == 0) ? 0 : 128;
            s.Band[band].sg[0] = d >> 15;
            for (i = 1; i < 7; i++)
            {
                s.Band[band].sg[i] = s.Band[band].d[i] >> 15;
                wd2 = (s.Band[band].sg[i] == s.Band[band].sg[0]) ? wd1 : -wd1;
                wd3 = (s.Band[band].b[i] * 32640) >> 15;
                s.Band[band].bp[i] = Saturate(wd2 + wd3);
            }

            // Block 4, DELAYA
            for (i = 6; i > 0; i--)
            {
                s.Band[band].d[i] = s.Band[band].d[i - 1];
                s.Band[band].b[i] = s.Band[band].bp[i];
            }

            for (i = 2; i > 0; i--)
            {
                s.Band[band].r[i] = s.Band[band].r[i - 1];
                s.Band[band].p[i] = s.Band[band].p[i - 1];
                s.Band[band].a[i] = s.Band[band].ap[i];
            }

            // Block 4, FILTEP
            wd1 = Saturate(s.Band[band].r[1] + s.Band[band].r[1]);
            wd1 = (s.Band[band].a[1] * wd1) >> 15;
            wd2 = Saturate(s.Band[band].r[2] + s.Band[band].r[2]);
            wd2 = (s.Band[band].a[2] * wd2) >> 15;
            s.Band[band].sp = Saturate(wd1 + wd2);

            // Block 4, FILTEZ
            s.Band[band].sz = 0;
            for (i = 6; i > 0; i--)
            {
                wd1 = Saturate(s.Band[band].d[i] + s.Band[band].d[i]);
                s.Band[band].sz += (s.Band[band].b[i] * wd1) >> 15;
            }
            s.Band[band].sz = Saturate(s.Band[band].sz);

            // Block 4, PREDIC
            s.Band[band].s = Saturate(s.Band[band].sp + s.Band[band].sz);
        }

        static readonly int[] wl = { -60, -30, 58, 172, 334, 538, 1198, 3042 };
        static readonly int[] rl42 = { 0, 7, 6, 5, 4, 3, 2, 1, 7, 6, 5, 4, 3, 2, 1, 0 };
        static readonly int[] ilb = { 2048, 2093, 2139, 2186, 2233, 2282, 2332, 2383, 2435, 2489, 2543, 2599, 2656, 2714, 2774, 2834, 2896, 2960, 3025, 3091, 3158, 3228, 3298, 3371, 3444, 3520, 3597, 3676, 3756, 3838, 3922, 4008 };
        static readonly int[] wh = { 0, -214, 798 };
        static readonly int[] rh2 = { 2, 1, 2, 1 };
        static readonly int[] qm2 = { -7408, -1616, 7408, 1616 };
        static readonly int[] qm4 = { 0, -20456, -12896, -8968, -6288, -4240, -2584, -1200, 20456, 12896, 8968, 6288, 4240, 2584, 1200, 0 };
        static readonly int[] qm5 = { -280, -280, -23352, -17560, -14120, -11664, -9752, -8184, -6864, -5712, -4696, -3784, -2960, -2208, -1520, -880, 23352, 17560, 14120, 11664, 9752, 8184, 6864, 5712, 4696, 3784, 2960, 2208, 1520, 880, 280, -280 };
        static readonly int[] qm6 = { -136, -136, -136, -136, -24808, -21904, -19008, -16704, -14984, -13512, -12280, -11192, -10232, -9360, -8576, -7856, -7192, -6576, -6000, -5456, -4944, -4464, -4008, -3576, -3168, -2776, -2400, -2032, -1688, -1360, -1040, -728, 24808, 21904, 19008, 16704, 14984, 13512, 12280, 11192, 10232, 9360, 8576, 7856, 7192, 6576, 6000, 5456, 4944, 4464, 4008, 3576, 3168, 2776, 2400, 2032, 1688, 1360, 1040, 728, 432, 136, -432, -136 };
        static readonly int[] qmf_coeffs = { 3, -11, 12, 32, -210, 951, 3876, -805, 362, -156, 53, -11, };
        static readonly int[] q6 = { 0, 35, 72, 110, 150, 190, 233, 276, 323, 370, 422, 473, 530, 587, 650, 714, 786, 858, 940, 1023, 1121, 1219, 1339, 1458, 1612, 1765, 1980, 2195, 2557, 2919, 0, 0 };
        static readonly int[] iln = { 0, 63, 62, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 0 };
        static readonly int[] ilp = { 0, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 0 };
        static readonly int[] ihn = { 0, 1, 0 };
        static readonly int[] ihp = { 0, 3, 2 };

        /// <summary>
        /// Decodes a buffer of G722
        /// </summary>
        /// <param name="state">Codec state</param>
        /// <param name="outputBuffer">Output buffer (to contain decompressed PCM samples)</param>
        /// <param name="inputG722Data"></param>
        /// <param name="inputLength">Number of bytes in input G722 data to decode</param>
        /// <returns>Number of samples written into output buffer</returns>
        public int Decode(G722CodecState state, short[] outputBuffer, byte[] inputG722Data, int inputLength)
        {
            int dlowt;
            int rlow;
            int ihigh;
            int dhigh;
            int rhigh;
            int xout1;
            int xout2;
            int wd1;
            int wd2;
            int wd3;
            int code;
            int outlen;
            int i;
            int j;

            outlen = 0;
            rhigh = 0;
            for (j = 0; j < inputLength; )
            {
                if (state.Packed)
                {
                    // Unpack the code bits
                    if (state.InBits < state.BitsPerSample)
                    {
                        state.InBuffer |= (uint)(inputG722Data[j++] << state.InBits);
                        state.InBits += 8;
                    }
                    code = (int)state.InBuffer & ((1 << state.BitsPerSample) - 1);
                    state.InBuffer >>= state.BitsPerSample;
                    state.InBits -= state.BitsPerSample;
                }
                else
                {
                    code = inputG722Data[j++];
                }

                switch (state.BitsPerSample)
                {
                    default:
                    case 8:
                        wd1 = code & 0x3F;
                        ihigh = (code >> 6) & 0x03;
                        wd2 = qm6[wd1];
                        wd1 >>= 2;
                        break;
                    case 7:
                        wd1 = code & 0x1F;
                        ihigh = (code >> 5) & 0x03;
                        wd2 = qm5[wd1];
                        wd1 >>= 1;
                        break;
                    case 6:
                        wd1 = code & 0x0F;
                        ihigh = (code >> 4) & 0x03;
                        wd2 = qm4[wd1];
                        break;
                }
                
                // Block 5L, LOW BAND INVQBL
                wd2 = (state.Band[0].det * wd2) >> 15;
                
                // Block 5L, RECONS
                rlow = state.Band[0].s + wd2;
                
                // Block 6L, LIMIT
                if (rlow > 16383)
                    rlow = 16383;
                else if (rlow < -16384)
                    rlow = -16384;

                // Block 2L, INVQAL
                wd2 = qm4[wd1];
                dlowt = (state.Band[0].det * wd2) >> 15;

                // Block 3L, LOGSCL
                wd2 = rl42[wd1];
                wd1 = (state.Band[0].nb * 127) >> 7;
                wd1 += wl[wd2];
                if (wd1 < 0)
                    wd1 = 0;
                else if (wd1 > 18432)
                    wd1 = 18432;
                state.Band[0].nb = wd1;

                // Block 3L, SCALEL
                wd1 = (state.Band[0].nb >> 6) & 31;
                wd2 = 8 - (state.Band[0].nb >> 11);
                wd3 = (wd2 < 0) ? (ilb[wd1] << -wd2) : (ilb[wd1] >> wd2);
                state.Band[0].det = wd3 << 2;

                Block4(state, 0, dlowt);

                if (!state.EncodeFrom8000Hz)
                {
                    // Block 2H, INVQAH
                    wd2 = qm2[ihigh];
                    dhigh = (state.Band[1].det * wd2) >> 15;
                    
                    // Block 5H, RECONS
                    rhigh = dhigh + state.Band[1].s;
                    
                    // Block 6H, LIMIT
                    if (rhigh > 16383)
                        rhigh = 16383;
                    else if (rhigh < -16384)
                        rhigh = -16384;

                    // Block 2H, INVQAH
                    wd2 = rh2[ihigh];
                    wd1 = (state.Band[1].nb * 127) >> 7;
                    wd1 += wh[wd2];
                    if (wd1 < 0)
                        wd1 = 0;
                    else if (wd1 > 22528)
                        wd1 = 22528;
                    state.Band[1].nb = wd1;

                    // Block 3H, SCALEH
                    wd1 = (state.Band[1].nb >> 6) & 31;
                    wd2 = 10 - (state.Band[1].nb >> 11);
                    wd3 = (wd2 < 0) ? (ilb[wd1] << -wd2) : (ilb[wd1] >> wd2);
                    state.Band[1].det = wd3 << 2;

                    Block4(state, 1, dhigh);
                }

                if (state.ItuTestMode)
                {
                    outputBuffer[outlen++] = (short)(rlow << 1);
                    outputBuffer[outlen++] = (short)(rhigh << 1);
                }
                else
                {
                    if (state.EncodeFrom8000Hz)
                    {
                        outputBuffer[outlen++] = (short)(rlow << 1);
                    }
                    else
                    {
                        // Apply the receive QMF
                        for (i = 0; i < 22; i++)
                            state.QmfSignalHistory[i] = state.QmfSignalHistory[i + 2];
                        state.QmfSignalHistory[22] = rlow + rhigh;
                        state.QmfSignalHistory[23] = rlow - rhigh;

                        xout1 = 0;
                        xout2 = 0;
                        for (i = 0; i < 12; i++)
                        {
                            xout2 += state.QmfSignalHistory[2 * i] * qmf_coeffs[i];
                            xout1 += state.QmfSignalHistory[2 * i + 1] * qmf_coeffs[11 - i];
                        }
                        outputBuffer[outlen++] = (short)(xout1 >> 11);
                        outputBuffer[outlen++] = (short)(xout2 >> 11);
                    }
                }
            }
            return outlen;
        }

        /// <summary>
        /// Encodes a buffer of G722
        /// </summary>
        /// <param name="state">Codec state</param>
        /// <param name="outputBuffer">Output buffer (to contain encoded G722)</param>
        /// <param name="inputBuffer">PCM 16 bit samples to encode</param>
        /// <param name="inputBufferCount">Number of samples in the input buffer to encode</param>
        /// <returns>Number of encoded bytes written into output buffer</returns>
        public int Encode(G722CodecState state, byte[] outputBuffer, short[] inputBuffer, int inputBufferCount)
        {
            int dlow;
            int dhigh;
            int el;
            int wd;
            int wd1;
            int ril;
            int wd2;
            int il4;
            int ih2;
            int wd3;
            int eh;
            int mih;
            int i;
            int j;
            // Low and high band PCM from the QMF
            int xlow;
            int xhigh;
            int g722_bytes;
            // Even and odd tap accumulators
            int sumeven;
            int sumodd;
            int ihigh;
            int ilow;
            int code;

            g722_bytes = 0;
            xhigh = 0;
            for (j = 0; j < inputBufferCount; )
            {
                if (state.ItuTestMode)
                {
                    xlow =
                    xhigh = inputBuffer[j++] >> 1;
                }
                else
                {
                    if (state.EncodeFrom8000Hz)
                    {
                        xlow = inputBuffer[j++] >> 1;
                    }
                    else
                    {
                        // Apply the transmit QMF
                        // Shuffle the buffer down
                        for (i = 0; i < 22; i++)
                            state.QmfSignalHistory[i] = state.QmfSignalHistory[i + 2];
                        state.QmfSignalHistory[22] = inputBuffer[j++];
                        state.QmfSignalHistory[23] = inputBuffer[j++];

                        // Discard every other QMF output
                        sumeven = 0;
                        sumodd = 0;
                        for (i = 0; i < 12; i++)
                        {
                            sumodd += state.QmfSignalHistory[2 * i] * qmf_coeffs[i];
                            sumeven += state.QmfSignalHistory[2 * i + 1] * qmf_coeffs[11 - i];
                        }
                        xlow = (sumeven + sumodd) >> 14;
                        xhigh = (sumeven - sumodd) >> 14;
                    }
                }
                // Block 1L, SUBTRA
                el = Saturate(xlow - state.Band[0].s);

                // Block 1L, QUANTL
                wd = (el >= 0) ? el : -(el + 1);

                for (i = 1; i < 30; i++)
                {
                    wd1 = (q6[i] * state.Band[0].det) >> 12;
                    if (wd < wd1)
                        break;
                }
                ilow = (el < 0) ? iln[i] : ilp[i];

                // Block 2L, INVQAL
                ril = ilow >> 2;
                wd2 = qm4[ril];
                dlow = (state.Band[0].det * wd2) >> 15;

                // Block 3L, LOGSCL
                il4 = rl42[ril];
                wd = (state.Band[0].nb * 127) >> 7;
                state.Band[0].nb = wd + wl[il4];
                if (state.Band[0].nb < 0)
                    state.Band[0].nb = 0;
                else if (state.Band[0].nb > 18432)
                    state.Band[0].nb = 18432;

                // Block 3L, SCALEL
                wd1 = (state.Band[0].nb >> 6) & 31;
                wd2 = 8 - (state.Band[0].nb >> 11);
                wd3 = (wd2 < 0) ? (ilb[wd1] << -wd2) : (ilb[wd1] >> wd2);
                state.Band[0].det = wd3 << 2;

                Block4(state, 0, dlow);

                if (state.EncodeFrom8000Hz)
                {
                    // Just leave the high bits as zero
                    code = (0xC0 | ilow) >> (8 - state.BitsPerSample);
                }
                else
                {
                    // Block 1H, SUBTRA
                    eh = Saturate(xhigh - state.Band[1].s);

                    // Block 1H, QUANTH
                    wd = (eh >= 0) ? eh : -(eh + 1);
                    wd1 = (564 * state.Band[1].det) >> 12;
                    mih = (wd >= wd1) ? 2 : 1;
                    ihigh = (eh < 0) ? ihn[mih] : ihp[mih];

                    // Block 2H, INVQAH
                    wd2 = qm2[ihigh];
                    dhigh = (state.Band[1].det * wd2) >> 15;

                    // Block 3H, LOGSCH
                    ih2 = rh2[ihigh];
                    wd = (state.Band[1].nb * 127) >> 7;
                    state.Band[1].nb = wd + wh[ih2];
                    if (state.Band[1].nb < 0)
                        state.Band[1].nb = 0;
                    else if (state.Band[1].nb > 22528)
                        state.Band[1].nb = 22528;

                    // Block 3H, SCALEH
                    wd1 = (state.Band[1].nb >> 6) & 31;
                    wd2 = 10 - (state.Band[1].nb >> 11);
                    wd3 = (wd2 < 0) ? (ilb[wd1] << -wd2) : (ilb[wd1] >> wd2);
                    state.Band[1].det = wd3 << 2;

                    Block4(state, 1, dhigh);
                    code = ((ihigh << 6) | ilow) >> (8 - state.BitsPerSample);
                }

                if (state.Packed)
                {
                    // Pack the code bits
                    state.OutBuffer |= (uint)(code << state.OutBits);
                    state.OutBits += state.BitsPerSample;
                    if (state.OutBits >= 8)
                    {
                        outputBuffer[g722_bytes++] = (byte)(state.OutBuffer & 0xFF);
                        state.OutBits -= 8;
                        state.OutBuffer >>= 8;
                    }
                }
                else
                {
                    outputBuffer[g722_bytes++] = (byte)code;
                }
            }
            return g722_bytes;
        }
    }

    /// <summary>
    /// Stores state to be used between calls to Encode or Decode
    /// </summary>
    public class G722CodecState
    {
        /// <summary>
        /// ITU Test Mode
        /// TRUE if the operating in the special ITU test mode, with the band split filters disabled.
        /// </summary>
        public bool ItuTestMode { get; set; }

        /// <summary>
        /// TRUE if the G.722 data is packed
        /// </summary>
        public bool Packed { get; private set; }

        /// <summary>
        /// 8kHz Sampling
        /// TRUE if encode from 8k samples/second
        /// </summary>
        public bool EncodeFrom8000Hz { get; private set; }

        /// <summary>
        /// Bits Per Sample
        /// 6 for 48000kbps, 7 for 56000kbps, or 8 for 64000kbps.
        /// </summary>
        public int BitsPerSample { get; private set; }

        /// <summary>
        /// Signal history for the QMF (x)
        /// </summary>
        public int[] QmfSignalHistory { get; private set; }

        /// <summary>
        /// Band
        /// </summary>
        public Band[] Band { get; private set; }

        /// <summary>
        /// In bit buffer
        /// </summary>
        public uint InBuffer { get; internal set; }

        /// <summary>
        /// Number of bits in InBuffer
        /// </summary>
        public int InBits { get; internal set; }

        /// <summary>
        /// Out bit buffer
        /// </summary>
        public uint OutBuffer { get; internal set; }

        /// <summary>
        /// Number of bits in OutBuffer
        /// </summary>
        public int OutBits { get; internal set; }

        /// <summary>
        /// Creates a new instance of G722 Codec State for a 
        /// new encode or decode session
        /// </summary>
        /// <param name="rate">Bitrate (typically 64000)</param>
        /// <param name="options">Special options</param>
        public G722CodecState(int rate, G722Flags options)
        {
            this.Band = new Band[2] { new Band(), new Band() };
            this.QmfSignalHistory = new int[24];
            this.ItuTestMode = false;

            if (rate == 48000)
                this.BitsPerSample = 6;
            else if (rate == 56000)
                this.BitsPerSample = 7;
            else if (rate == 64000)
                this.BitsPerSample = 8;
            else
                throw new ArgumentException("Invalid rate, should be 48000, 56000 or 64000");
            if ((options & G722Flags.SampleRate8000) == G722Flags.SampleRate8000)
                this.EncodeFrom8000Hz = true;
            if (((options & G722Flags.Packed) == G722Flags.Packed) && this.BitsPerSample != 8)
                this.Packed = true;
            else
                this.Packed = false;
            this.Band[0].det = 32;
            this.Band[1].det = 8;
        }
    }

    /// <summary>
    /// Band data for G722 Codec
    /// </summary>
    public class Band
    {
        /// <summary>s</summary>
        public int s;
        /// <summary>sp</summary>
        public int sp;
        /// <summary>sz</summary>
        public int sz;
        /// <summary>r</summary>
        public int[] r = new int[3];
        /// <summary>a</summary>
        public int[] a = new int[3];
        /// <summary>ap</summary>
        public int[] ap = new int[3];
        /// <summary>p</summary>
        public int[] p = new int[3];
        /// <summary>d</summary>
        public int[] d = new int[7];
        /// <summary>b</summary>
        public int[] b = new int[7];
        /// <summary>bp</summary>
        public int[] bp = new int[7];
        /// <summary>sg</summary>
        public int[] sg = new int[7];
        /// <summary>nb</summary>
        public int nb;
        /// <summary>det</summary>
        public int det;
    }

    /// <summary>
    /// G722 Flags
    /// </summary>
    [Flags]
    public enum G722Flags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Using a G722 sample rate of 8000
        /// </summary>
        SampleRate8000 = 0x0001,
        /// <summary>
        /// Packed
        /// </summary>
        Packed = 0x0002
    }
}
