using System;
using System.Collections.Generic;
namespace NAudio.Wave.Alsa
{
    public class AlsaDriver
    {
        private IntPtr _playbackPcm;
        private List<AlsaCard> _cards = new List<AlsaCard>();
        public AlsaDriver()
        {
            EnumerateCards();
        }
        public void EnumerateCards()
        {
            int card = -1;
            if (AlsaInterop.NextCard(ref card) < 0 || card < 0)
            {
                throw new Exception("no soundcards found");
            }
            while (card >= 0){
                string name = $"hw:{card.ToString()}";
                if (AlsaCard.Create(name, out AlsaCard card_obj))
                {
                    _cards.Add(card_obj);
                    Console.WriteLine(name);
                }
                AlsaInterop.NextCard(ref card);
            }
        }
        internal static PCMFormat GetFormat(WaveFormat format)
        {
            switch (format.Encoding)
            {
                case WaveFormatEncoding.IeeeFloat:
                    switch (format.BitsPerSample)
                    {
                        case 32:
                            return PCMFormat.SND_PCM_FORMAT_FLOAT_LE;
                    }
                    break;
                case WaveFormatEncoding.Pcm:
                    switch (format.BitsPerSample)
                    {
                        case 32:
                            return PCMFormat.SND_PCM_FORMAT_S32_LE;
                        case 24:
                            return PCMFormat.SND_PCM_FORMAT_S24_3LE;
                        case 16:
                            return PCMFormat.SND_PCM_FORMAT_S16_LE;
                        case 8:
                            return PCMFormat.SND_PCM_FORMAT_S8;
                    }
                    break;
                case WaveFormatEncoding.MuLaw:
                    return PCMFormat.SND_PCM_FORMAT_MU_LAW;
                case WaveFormatEncoding.ALaw:
                    return PCMFormat.SND_PCM_FORMAT_A_LAW;
            }
            return PCMFormat.SND_PCM_FORMAT_UNKNOWN;
        }
    }
}