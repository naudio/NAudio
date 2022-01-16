using System;
using System.IO;
using NAudio.MediaFoundation;
namespace MediaFoundationPlay
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the name of the file to play.");
            string filename = Console.ReadLine();
            if (filename.StartsWith("\"")) filename=filename.Remove(0, 1);
            if (filename.EndsWith("\"")) filename = filename.Remove(filename.Length - 1);
            if (!File.Exists(filename))
            {
                Console.WriteLine("File not found.");
                Environment.Exit(0);
            }
            MediaFoundationProvider provider = new MediaFoundationProvider(filename);
            MediaFoundationPlayer player = new MediaFoundationPlayer();
            player.Init(provider);
            while (!player.Prepared) { }
            player.PlayFromBegining();
            while(player.PlaybackState != NAudio.Wave.PlaybackState.Stopped){}
        }
    }
}
