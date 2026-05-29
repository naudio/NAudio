using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.ConvolutionReverbDemo
{
    /// <summary>
    /// Offline convolution-reverb test bench: render an input file through every IR
    /// in a folder (one at a time or batched), with IR auto-resample to the input's
    /// rate and peak-normalisation to -3 dBFS. Reports Nx-real-time and added tail
    /// duration. Output WAVs land in a temp folder browsable from the panel.
    /// </summary>
    class ConvolutionReverbDemoViewModel : ViewModelBase
    {
        private const float TargetPeakDbFs = -3f;
        private static readonly float TargetPeakLinear = MathF.Pow(10f, TargetPeakDbFs / 20f);

        private string inputFilePath;
        private string irFolderPath;
        private string selectedIr;
        private string selectedRender;
        private string status = "Pick an input file and a folder of impulse responses.";
        private bool busy;

        public ConvolutionReverbDemoViewModel()
        {
            OutputFolder = Path.Combine(Path.GetTempPath(), "NAudioWpfDemo", "ConvolutionReverb");
            Directory.CreateDirectory(OutputFolder);

            Irs = new ObservableCollection<string>();
            Renders = new ObservableCollection<string>();
            RefreshRenders();

            ChooseInputCommand = new DelegateCommand(ChooseInput);
            ChooseIrFolderCommand = new DelegateCommand(ChooseIrFolder);
            ProcessSelectedCommand = new DelegateCommand(() => _ = ProcessAsync(false));
            ProcessAllCommand = new DelegateCommand(() => _ = ProcessAsync(true));
            PlayRenderCommand = new DelegateCommand(PlayRender);
            DeleteRenderCommand = new DelegateCommand(DeleteRender);
            OpenOutputFolderCommand = new DelegateCommand(() => ShellExecute(OutputFolder));

            UpdateCommands();
        }

        public string OutputFolder { get; }
        public ObservableCollection<string> Irs { get; }
        public ObservableCollection<string> Renders { get; }

        public string InputFilePath
        {
            get => inputFilePath;
            private set { inputFilePath = value; OnPropertyChanged(nameof(InputFilePath)); UpdateCommands(); }
        }
        public string IrFolderPath
        {
            get => irFolderPath;
            private set { irFolderPath = value; OnPropertyChanged(nameof(IrFolderPath)); }
        }
        public string SelectedIr
        {
            get => selectedIr;
            set { selectedIr = value; OnPropertyChanged(nameof(SelectedIr)); UpdateCommands(); }
        }
        public string SelectedRender
        {
            get => selectedRender;
            set { selectedRender = value; OnPropertyChanged(nameof(SelectedRender)); UpdateCommands(); }
        }
        public string Status
        {
            get => status;
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public DelegateCommand ChooseInputCommand { get; }
        public DelegateCommand ChooseIrFolderCommand { get; }
        public DelegateCommand ProcessSelectedCommand { get; }
        public DelegateCommand ProcessAllCommand { get; }
        public DelegateCommand PlayRenderCommand { get; }
        public DelegateCommand DeleteRenderCommand { get; }
        public DelegateCommand OpenOutputFolderCommand { get; }

        private void ChooseInput()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
                InputFilePath = dialog.FileName;
        }

        private void ChooseIrFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() != true)
                return;
            IrFolderPath = dialog.FolderName;
            Irs.Clear();
            foreach (var f in Directory.EnumerateFiles(IrFolderPath, "*.wav").OrderBy(f => f))
                Irs.Add(Path.GetFileName(f));
            SelectedIr = Irs.FirstOrDefault();
            UpdateCommands();
        }

        private async Task ProcessAsync(bool all)
        {
            if (busy || InputFilePath == null)
                return;
            busy = true;
            UpdateCommands();
            try
            {
                var targets = all ? Irs.ToList() : new List<string> { SelectedIr };
                foreach (var irName in targets)
                {
                    if (irName == null)
                        continue;
                    Status = $"Processing {irName}…";
                    try
                    {
                        var report = await Task.Run(() => RenderOne(InputFilePath, Path.Combine(IrFolderPath, irName)));
                        Status = $"{irName}: {report}";
                        RefreshRenders();
                    }
                    catch (Exception ex)
                    {
                        Status = $"Failed on {irName}: {ex.Message}";
                    }
                }
            }
            finally
            {
                busy = false;
                UpdateCommands();
            }
        }

        private string RenderOne(string inputPath, string irPath)
        {
            using var reader = new AudioFileReader(inputPath);
            var inputRate = reader.WaveFormat.SampleRate;
            var inputChannels = reader.WaveFormat.Channels;

            var ir = LoadAndPrepareIr(irPath, inputRate);
            var irChannels = ir.Length;
            var irLength = ir[0].Length;
            var outputChannels = Math.Max(inputChannels, irChannels);
            if (outputChannels > 2)
                throw new NotSupportedException("Only mono and stereo inputs/IRs are supported by this demo.");

            ISampleProvider source = reader;
            if (inputChannels == 1 && outputChannels == 2)
                source = new MonoToStereoSampleProvider(reader);

            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputRate, outputChannels);
            var effect = new ConvolutionReverbEffect { Mix = 1f };
            effect.Configure(outputFormat);
            effect.SetImpulseResponse(ir);

            var latency = effect.LatencySamples;
            var outName = $"{Path.GetFileNameWithoutExtension(inputPath)}_" +
                          $"{Path.GetFileNameWithoutExtension(irPath)}_" +
                          $"{DateTime.Now:HHmmss}.wav";
            var outPath = Path.Combine(OutputFolder, outName);

            var stopwatch = Stopwatch.StartNew();
            long inputFrames = 0;
            using (var writer = new WaveFileWriter(outPath, outputFormat))
            {
                const int blockFrames = 4096;
                var buffer = new float[blockFrames * outputChannels];
                long framesProcessed = 0;

                while (true)
                {
                    var read = source.Read(buffer);
                    if (read == 0)
                        break;
                    inputFrames += read / outputChannels;
                    effect.Process(buffer.AsSpan(0, read));
                    WriteSkippingLatency(writer, buffer, read, outputChannels, ref framesProcessed, latency);
                }

                // Flush the tail: feed (irLength + latency) silent frames so the full
                // tail emerges after the initial latency-skip window.
                var tailFramesNeeded = irLength + latency;
                long tailFramesFed = 0;
                while (tailFramesFed < tailFramesNeeded)
                {
                    var framesThisRound = (int)Math.Min(blockFrames, tailFramesNeeded - tailFramesFed);
                    var samples = framesThisRound * outputChannels;
                    Array.Clear(buffer, 0, samples);
                    effect.Process(buffer.AsSpan(0, samples));
                    WriteSkippingLatency(writer, buffer, samples, outputChannels, ref framesProcessed, latency);
                    tailFramesFed += framesThisRound;
                }
            }
            stopwatch.Stop();

            var inputSeconds = inputFrames / (double)inputRate;
            var tailSeconds = irLength / (double)inputRate;
            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var nx = elapsedSeconds > 0 ? inputSeconds / elapsedSeconds : double.PositiveInfinity;
            return $"{nx:0.#}× real-time, +{tailSeconds:0.##} s tail";
        }

        private static void WriteSkippingLatency(WaveFileWriter writer, float[] buffer, int samples,
            int channels, ref long framesProcessed, int latencyFrames)
        {
            var framesInBuffer = samples / channels;
            var skipFrames = (int)Math.Min(framesInBuffer, Math.Max(0L, latencyFrames - framesProcessed));
            framesProcessed += framesInBuffer;
            if (skipFrames < framesInBuffer)
                writer.WriteSamples(buffer, skipFrames * channels, (framesInBuffer - skipFrames) * channels);
        }

        private static float[][] LoadAndPrepareIr(string path, int targetSampleRate)
        {
            using var reader = new AudioFileReader(path);
            ISampleProvider provider = reader;
            if (reader.WaveFormat.SampleRate != targetSampleRate)
                provider = new WdlResamplingSampleProvider(provider, targetSampleRate);

            var channels = provider.WaveFormat.Channels;
            var pool = new List<float>(1 << 16);
            var temp = new float[4096];
            int read;
            while ((read = provider.Read(temp)) > 0)
                for (var i = 0; i < read; i++)
                    pool.Add(temp[i]);

            var totalFrames = pool.Count / channels;
            var perChannel = new float[channels][];
            for (var ch = 0; ch < channels; ch++)
            {
                var arr = new float[totalFrames];
                for (var i = 0; i < totalFrames; i++)
                    arr[i] = pool[i * channels + ch];
                perChannel[ch] = arr;
            }

            // Peak-normalise the IR to -3 dBFS across all channels (preserves stereo balance).
            var peak = 0f;
            foreach (var arr in perChannel)
                foreach (var s in arr)
                    if (MathF.Abs(s) > peak)
                        peak = MathF.Abs(s);
            if (peak > 1e-9f)
            {
                var scale = TargetPeakLinear / peak;
                foreach (var arr in perChannel)
                    for (var i = 0; i < arr.Length; i++)
                        arr[i] *= scale;
            }
            return perChannel;
        }

        private void RefreshRenders()
        {
            Renders.Clear();
            foreach (var f in Directory.EnumerateFiles(OutputFolder, "*.wav").OrderBy(f => f))
                Renders.Add(Path.GetFileName(f));
        }

        private void PlayRender()
        {
            if (SelectedRender != null)
                ShellExecute(Path.Combine(OutputFolder, SelectedRender));
        }

        private void DeleteRender()
        {
            if (SelectedRender == null)
                return;
            try
            {
                File.Delete(Path.Combine(OutputFolder, SelectedRender));
                Renders.Remove(SelectedRender);
                SelectedRender = Renders.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not delete render: " + ex.Message);
            }
        }

        private static void ShellExecute(string target)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(target) { UseShellExecute = true }
            };
            p.Start();
        }

        private void UpdateCommands()
        {
            ProcessSelectedCommand.IsEnabled = !busy && InputFilePath != null && SelectedIr != null;
            ProcessAllCommand.IsEnabled = !busy && InputFilePath != null && Irs.Count > 0;
            PlayRenderCommand.IsEnabled = SelectedRender != null;
            DeleteRenderCommand.IsEnabled = SelectedRender != null;
        }
    }
}
