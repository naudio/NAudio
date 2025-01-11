using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;

namespace NAudioSdl2Demo
{
    public class RecordingDemo
    {
        private WaveFileWriter waveWriter;
        private WaveInSdl waveIn;
        private double secondsRecorded;
        private CancellationTokenSource cancellationTokenSource;

        public async Task Start()
        {
            Console.Clear();
            Console.WriteLine("*** WaveInSdl Demo ***");
            Console.WriteLine("Devices:");
            var capabilitiesList = WaveInSdl.GetCapabilitiesList();
            capabilitiesList.Dump();

            Console.Write("Choose device id: ");
            var deviceId = Convert.ToInt32(Console.ReadLine());
            var deviceCapabilities = capabilitiesList.First(x => x.DeviceNumber == deviceId);

            Console.Write("Choose recording time (seconds): ");
            var recordSeconds = Convert.ToInt32(Console.ReadLine());

            Console.Write("Wav file absolute path: ");
            var fileAbsolutePath = Console.ReadLine();

            cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            waveIn = new WaveInSdl();
            waveIn.WaveFormat = new WaveFormat(deviceCapabilities.Frequency, deviceCapabilities.Bits, deviceCapabilities.Channels);
            waveIn.DeviceId = deviceId;
            waveIn.DataAvailable += WaveInOnDataAvailable;
            waveWriter = new WaveFileWriter(fileAbsolutePath, waveIn.WaveFormat);
            waveIn.StartRecording();
            await waveWriter.WaitEnd(waveIn.BufferMilliseconds, recordSeconds, cancellationTokenSource.Token);
            waveIn.DataAvailable -= WaveInOnDataAvailable;
            waveIn.StopRecording();
            waveIn.Dispose();
            waveWriter.Dispose();

            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
            cancellationTokenSource.Dispose();
        }

        private void WaveInOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            secondsRecorded += waveIn.BufferMilliseconds / 1000.0;
            waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            ConsoleHelper.LockCursorPosition("TotalTime");
            Console.WriteLine($"WaveInSdl seconds recorded: {secondsRecorded.ToString("F1")}");
            ConsoleHelper.LockCursorPosition("PeakLevel");
            Console.WriteLine($"WaveInSdl peak level: {(waveIn.PeakLevel * 100).ToString("00.00")}%");
            
        }

        private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        }
    }

    public static class RecordingDemoExtensions
    {
        public static async Task WaitEnd(
            this WaveFileWriter waveWriter,
            int delay,
            int recordSeconds,
            CancellationToken cancellationToken)
        {
            while (waveWriter.TotalTime.TotalSeconds < recordSeconds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                await Task.Delay(delay);
            }
        }

        public static void Dump(this List<WaveInSdlCapabilities> list)
        {
            foreach (var device in list)
            {
                Console.WriteLine(device.ToString(Environment.NewLine));
                Console.WriteLine();
            }
        }
    }
}
