using System;
using System.Collections.Generic;
using NAudio.Wave;
namespace NAudio.Wave.Alsa
{
    public class AlsaCard : IDisposable
    {
        public string IdString { get; private set; }
        public int Index { get; private set; }
        public string Id { get; private set; }
        public string Driver { get; private set; }
        public string Name { get; private set; }
        public string LongName { get; private set; }
        public string MixerName { get; private set; }
        public string Components { get; private set; }
        private IntPtr handle;
        public IntPtr Handle 
        {
            get => handle;
        }
        public List<AlsaOut> OutputDevices { get; private set; } = new List<AlsaOut>();    
        public string[] GetDeviceNames()
        {
            return new string[] { "" };
        }
        private AlsaCard(IntPtr handle) 
        {
            this.handle = handle;
        }
        public static unsafe bool Create(string name, out AlsaCard card)
        {
            int error;
            card = null;
            IntPtr info = default;
            AlsaInterop.CtlCardInfoMalloc(ref info);
            if ((error = AlsaInterop.CtlOpen(out IntPtr handle, name, 0)) < 0)
            {
                AlsaInterop.CtlCardInfoFree(info);
                Console.WriteLine(AlsaInterop.ErrorString(error));
                return false;
            }
            if ((error = AlsaInterop.CtlCardInfo(handle, info)) < 0)
            {
                AlsaInterop.CtlCardInfoFree(info);
                Console.WriteLine(AlsaInterop.ErrorString(error));
                AlsaInterop.CtlClose(handle);
                return false;
            }
            card = new AlsaCard(handle);
            card.IdString = name;
            int dev = -1;
            do 
            {
                if ((error = AlsaInterop.CtlPcmNextDevice(handle, ref dev)) < 0)
                {
                    AlsaInterop.CtlCardInfoFree(info);
                    Console.WriteLine(AlsaInterop.ErrorString(error));
                    return false;
                }
                if (dev < 0)
                {
                    break;
                }
                if (AlsaOut.Create(card, dev, out AlsaOut device))
                {
                    card.OutputDevices.Add(device);
                }
            } while (true);
            card.Name = AlsaInterop.CtlCardInfoGetName(info);
            card.Index = AlsaInterop.CtlCardInfoGetCard(info);
            card.Id = AlsaInterop.CtlCardInfoGetID(info);
            card.Driver = AlsaInterop.CtlCardInfoGetDriver(info);
            card.LongName = AlsaInterop.CtlCardInfoGetLongName(info);
            card.MixerName = AlsaInterop.CtlCardInfoGetMixerName(info); 
            card.Components = AlsaInterop.CtlCardInfoGetComponents(info); 
            AlsaInterop.CtlCardInfoFree(info);
            return true;
        }
        public void Dispose()
        {
            AlsaInterop.CtlClose(handle);
        }
        public override string ToString()
        {
            return $"card {Index}: {Id} [{Name}]";
        }
        ~AlsaCard()
        {
            Console.WriteLine("Disposing ALSA Card");
            Dispose();
        }
    }
}