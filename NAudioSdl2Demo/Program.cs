using static NAudio.Sdl2.Interop.SDL;

namespace NAudioSdl2Demo
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            int demoTypeId = -1;
            do
            {
                Console.Clear();
                try
                {
                    Console.WriteLine("*** SDL2 Demo ***");
                    Console.WriteLine("SDL2 Demo Options");
                    Console.WriteLine("Playback: 0");
                    Console.WriteLine("Recording: 1");
                    Console.WriteLine("Quit: -1");
                    Console.WriteLine();
                    Console.Write("Choose demo id: ");
                    demoTypeId = Convert.ToInt32(Console.ReadLine());
                    if (demoTypeId == 0)
                    {
                        var playbackDemo = new PlaybackDemo();
                        await playbackDemo.Start();
                    }
                    if (demoTypeId == 1)
                    {
                        var recordingDemo = new RecordingDemo();
                        await recordingDemo.Start();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
                Console.WriteLine();
                Console.WriteLine("Press any key");
                Console.ReadKey();

            } while (demoTypeId != -1);
            Console.ReadLine();
        }
    }
}