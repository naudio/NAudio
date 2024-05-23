using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;

namespace NAudioSdl2Demo
{
    public class PlaybackDemo
    {
        private WaveFileReader waveReader;
        private WaveOutSdl waveOut;
        private CancellationTokenSource cancellationTokenSource;

        public async Task Start()
        {
            Console.Clear();
            Console.WriteLine("*** WaveInSdl Demo ***");

            Console.WriteLine("Devices:");
            var capabilitiesList = WaveOutSdl.GetCapabilitiesList();
            capabilitiesList.Dump();

            Console.Write("Choose device id: ");
            var deviceId = Convert.ToInt32(Console.ReadLine());

            Console.Write("Wav file absolute path: ");
            var fileAbsolutePath = Console.ReadLine();

            cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            waveOut = new WaveOutSdl();
            waveOut.AudioConversion = AudioConversion.None;
            waveOut.DeviceId = deviceId;
            waveReader = new WaveFileReader(fileAbsolutePath);
            waveOut.Init(waveReader);
            waveOut.Play();
            await waveReader.WaitEnd(waveOut.DesiredLatency, WaveOutPositionChanged, cancellationTokenSource.Token);
            waveOut.Stop();
            waveOut.Dispose();
            waveReader.Dispose();
            
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
            cancellationTokenSource.Dispose();
        }

        private void WaveOutPositionChanged(TimeSpan currentTime, TimeSpan totalTime)
        {
            ConsoleHelper.LockCursorPosition("Position");
            Console.WriteLine($"Wave file position: {currentTime.ToString(@"hh\:mm\:ss\.fff")}/{totalTime.ToString(@"hh\:mm\:ss\.fff")}");
        }

        private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        }
    }

    public static class PlaybackDemoExtensions
    {
        public static async Task WaitEnd(
            this WaveFileReader waveReader, 
            int delay, 
            Action<TimeSpan, TimeSpan> positionChangedCallback, 
            CancellationToken cancellationToken)
        {
            while (waveReader.CurrentTime < waveReader.TotalTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                positionChangedCallback(waveReader.CurrentTime, waveReader.TotalTime);
                await Task.Delay(delay);
            }
            positionChangedCallback(waveReader.CurrentTime, waveReader.TotalTime);
        }

        public static void Dump(this List<WaveOutSdlCapabilities> list)
        {
            foreach (var device in list)
            {
                Console.WriteLine(device.ToString(Environment.NewLine));
                Console.WriteLine();
            }
        }
    }
}
