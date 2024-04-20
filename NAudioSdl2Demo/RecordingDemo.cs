using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;

namespace NAudioSdl2Demo
{
    public class RecordingDemo
    {
        private WaveFileWriter _waveWriter;
        private WaveInSdl _waveIn;
        private CancellationTokenSource _cancellationTokenSource;
        private (int Left, int Top)? _secondsRecorderPosition;
        private (int Left, int Top)? _peakLevelPosition;

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
            Console.Write("Choose recording time (seconds): ");
            var maxRecordSeconds = Convert.ToInt32(Console.ReadLine());
            Console.Write("Wav file absolute path: ");
            var fileAbsolutePath = Console.ReadLine();
            _cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            _waveIn = new WaveInSdl();
            _waveIn.AudioConversion = AudioConversion.Any;
            _waveIn.DeviceId = deviceId;
            _waveIn.DataAvailable += WaveInOnDataAvailable;
            _waveWriter = new WaveFileWriter(fileAbsolutePath, _waveIn.WaveFormat);
            _waveIn.StartRecording();
            await WaitRecordFinish(maxRecordSeconds);
            _waveIn.DataAvailable -= WaveInOnDataAvailable;
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveWriter.Dispose();
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        }

        private async Task WaitRecordFinish(int maxRecordSeconds)
        {
            var recordSeconds = 0;
            while (recordSeconds < maxRecordSeconds)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(1000);
                recordSeconds += 1;
            }
        }

        private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void WaveInOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            _secondsRecorderPosition ??= Console.GetCursorPosition();
            Console.SetCursorPosition(_secondsRecorderPosition.Value.Left, _secondsRecorderPosition.Value.Top);
            Console.WriteLine($"WaveInSdl seconds recorded: {_waveWriter.TotalTime.TotalSeconds}");
            _peakLevelPosition ??= Console.GetCursorPosition();
            Console.SetCursorPosition(_peakLevelPosition.Value.Left, _peakLevelPosition.Value.Top);
            Console.WriteLine($"WaveInSdl peak level: {_waveIn.PeakLevel}");
        }

        private void ShowDevicesInfo()
        {
            var deviceNumber = WaveInSdl.DeviceCount;
            for (int index = 0; index < deviceNumber; index++)
            {
                var deviceCapabilities = WaveInSdl.GetCapabilities(index);
                Console.WriteLine($"Id: {index}");
                Console.WriteLine($"Name: {deviceCapabilities.DeviceName}");
                Console.WriteLine();
            }
        }
    }
}
