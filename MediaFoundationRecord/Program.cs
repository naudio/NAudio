using System;
using System.Collections.Generic;
using System.Threading;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace MediaFoundationRecord
{
    class Program
    {
        static void Main(string[] args)
        {
            Record recoud = new Record();
        }
        
    }
    class Record
    {
        public Record()
        {
            List<IMFActivate> devices = new List<IMFActivate>();
            foreach (var i in MediaFoundationApi.EnumDeviceSources())
            {
                devices.Add(i);               
            }
            MediaFoundationCapturer capturer = new MediaFoundationCapturer(devices[0]);
            WaveInProvider waveIn = new WaveInProvider(capturer);
            capturer.StartRecording();
            Thread.Sleep(5000);
            capturer.StopRecording();
            MediaFoundationEncoder.EncodeToWma(waveIn, @"C:\record.mp3");
        }
    }
}
