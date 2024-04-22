using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;

namespace NAudioSdl2Demo
{
    public class PlaybackDemo
    {
        private WaveFileReader _waveReader;
        private WaveOutSdl _waveOut;
        private CancellationTokenSource _cancellationTokenSource;
        private (int Left, int Top)? _waveOutPosition;

        public async Task Start()
        {
            Console.Clear();
            Console.WriteLine("*** WaveInSdl Demo ***");
            Console.WriteLine("Devices:");
            ShowDevicesInfo();
            Console.Write("Choose device id: ");
            var deviceId = Convert.ToInt32(Console.ReadLine());
            if (deviceId < 0 || deviceId >= WaveOutSdl.DeviceCount)
                throw new InvalidOperationException("Device id out of range");
            Console.Write("Wav file absolute path: ");
            var fileAbsolutePath = Console.ReadLine();
            _cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            _waveOut = new WaveOutSdl();
            _waveOut.AudioConversion = AudioConversion.Any;
            _waveOut.DeviceId = deviceId;
            _waveOut.PositionChanged += WaveOutPositionChanged;
            _waveReader = new WaveFileReader(fileAbsolutePath);
            _waveOut.Init(_waveReader);
            _waveOut.Play();
            await WaitPlayFinish(_waveReader.TotalTime.TotalSeconds);
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveReader.Dispose();
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        }

        private async Task WaitPlayFinish(double waitSeconds)
        {
            var playSeconds = 0;
            while (playSeconds < waitSeconds)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(1000);
                playSeconds += 1;
            }
        }

        private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void WaveOutPositionChanged(object? sender, PositionChangedEventArgs e)
        {
            _waveOutPosition ??= Console.GetCursorPosition();
            Console.SetCursorPosition(_waveOutPosition.Value.Left, _waveOutPosition.Value.Top);
            Console.WriteLine($"WaveInOut position {_waveReader.CurrentTime.ToString(@"hh\:mm\:ss\.fff")}/{_waveReader.TotalTime.ToString(@"hh\:mm\:ss\.fff")}");
        }

        private void ShowDevicesInfo()
        {
            var deviceNumber = WaveOutSdl.DeviceCount;
            for (int index = 0; index < deviceNumber; index++)
            {
                var deviceCapabilities = WaveOutSdl.GetCapabilities(index);
                Console.WriteLine($"Id: {index}");
                Console.WriteLine($"Name: {deviceCapabilities.DeviceName}");
                Console.WriteLine();
            }
        }
    }
}
