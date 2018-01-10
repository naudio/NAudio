using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using MS.Internal.Xml.XPath;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NAudioWpfDemo.MediaFoundationEncode
{
    internal class MediaFoundationEncodeViewModel : ViewModelBase, IDisposable
    {
        private readonly Dictionary<Guid, List<MediaTypeViewModel>> allMediaTypes;
        private EncoderViewModel selectedOutputFormat;
        private MediaTypeViewModel selectedMediaType;
        private string inputFile;
        private string inputFormat;
        private WaveFormat inputWaveFormat;

        public List<EncoderViewModel> OutputFormats { get; }
        public List<MediaTypeViewModel> SupportedMediaTypes { get; private set; }
        public ICommand EncodeCommand { get; }
        public ICommand SelectInputFileCommand { get; }

        public MediaFoundationEncodeViewModel()
        {
            MediaFoundationApi.Startup();
            allMediaTypes = new Dictionary<Guid, List<MediaTypeViewModel>>();
            SupportedMediaTypes = new List<MediaTypeViewModel>();
            EncodeCommand = new DelegateCommand(Encode);
            SelectInputFileCommand = new DelegateCommand(SelectInputFile);

            // TODO: fill this by asking the encoders what they can do
            OutputFormats = new List<EncoderViewModel>();
            OutputFormats.Add(new EncoderViewModel() { Name = "AAC", Guid = AudioSubtypes.MFAudioFormat_AAC, Extension=".mp4" }); // Windows 8 can do a .aac extension as well
            OutputFormats.Add(new EncoderViewModel() { Name = "Windows Media Audio", Guid = AudioSubtypes.MFAudioFormat_WMAudioV8, Extension = ".wma"});
            OutputFormats.Add(new EncoderViewModel() { Name = "Windows Media Audio Professional", Guid = AudioSubtypes.MFAudioFormat_WMAudioV9, Extension = ".wma" });
            OutputFormats.Add(new EncoderViewModel() { Name = "MP3", Guid = AudioSubtypes.MFAudioFormat_MP3, Extension = ".mp3" });
            OutputFormats.Add(new EncoderViewModel() { Name = "Windows Media Audio Voice", Guid = AudioSubtypes.MFAudioFormat_MSP1, Extension = ".wma" });
            OutputFormats.Add(new EncoderViewModel() { Name = "Windows Media Audio Lossless", Guid = AudioSubtypes.MFAudioFormat_WMAudio_Lossless, Extension = ".wma" });
            OutputFormats.Add(new EncoderViewModel() { Name = "FLAC", Guid = Guid.Parse("0000f1ac-0000-0010-8000-00aa00389b71"), Extension = ".flac" });
            OutputFormats.Add(new EncoderViewModel() { Name = "Apple Lossless (ALAC)", Guid = Guid.Parse("63616c61-0000-0010-8000-00aa00389b71"), Extension = ".m4a" });
            OutputFormats.Add(new EncoderViewModel() { Name = "Fake for testing", Guid = Guid.NewGuid(), Extension = ".xyz" });
            SelectedOutputFormat = OutputFormats[0];
        }

        private void SelectInputFile()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Audio files|*.mp3;*.wav;*.wma;*.aiff;*.aac";
            if (ofd.ShowDialog() == true)
            {
                if (TryOpenInputFile(ofd.FileName))
                {
                    InputFile = ofd.FileName;
                    SetMediaTypes();
                }
            }
        }

        private bool TryOpenInputFile(string file)
        {
            bool isValid = false;
            try
            {
                using (var reader = new MediaFoundationReader(file))
                {
                    InputFormat = reader.WaveFormat.ToString();
                    inputWaveFormat = reader.WaveFormat;
                    isValid = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Not a supported input file ({0})", e.Message));
            }
            return isValid;
        }

        public string InputFile
        {
            get => inputFile;
            set
            {
                if (inputFile != value)
                {
                    inputFile = value;
                    OnPropertyChanged("InputFile");
                }
            }
        }

        public string InputFormat
        {
            get => inputFormat;
            set
            {
                if (inputFormat != value)
                {
                    inputFormat = value;
                    OnPropertyChanged("InputFormat");
                }
            }
        }

        public EncoderViewModel SelectedOutputFormat
        {
            get => selectedOutputFormat;
            set { 
                if (selectedOutputFormat != value)
                {
                    selectedOutputFormat = value;
                    SetMediaTypes();
                    OnPropertyChanged("SelectedOutputFormat");
                }
            }
        }

        private void SetMediaTypes()
        {
            if (!allMediaTypes.ContainsKey(SelectedOutputFormat.Guid))
            {
                TryGetSupportedMediaTypes();
            }
            FilterSupportedMediaTypes();
            OnPropertyChanged("SupportedMediaTypes");
            SelectedMediaType = SupportedMediaTypes.FirstOrDefault();
        }

        private void FilterSupportedMediaTypes()
        {
            //SupportedMediaTypes.Clear();
            SupportedMediaTypes = new List<MediaTypeViewModel>();
            if (inputWaveFormat == null)
            {
                SupportedMediaTypes.Add(new MediaTypeViewModel() {Name="Select an input file"});
                return;
            }
            SupportedMediaTypes = allMediaTypes[SelectedOutputFormat.Guid]
                .Where(m => m.MediaType != null)
                .Where(m => m.MediaType.SampleRate == inputWaveFormat.SampleRate)
                .Where(m => m.MediaType.ChannelCount == inputWaveFormat.Channels)
                .ToList();
        }

        private void TryGetSupportedMediaTypes()
        {
            var list = MediaFoundationEncoder.GetOutputMediaTypes(SelectedOutputFormat.Guid)
                                  .Select(mf => new MediaTypeViewModel(mf))
                                  .ToList();
            if (list.Count == 0)
            {
                list.Add(new MediaTypeViewModel() {Name = "Not Supported", Description = "No encoder found for this output type"});
            }
            allMediaTypes[SelectedOutputFormat.Guid] = list;
        }

        public MediaTypeViewModel SelectedMediaType 
        {
            get => selectedMediaType;
            set
            {
                if (selectedMediaType != value)
                {
                    selectedMediaType = value;
                    OnPropertyChanged("SelectedMediaType");
                }
            }
        }

        private void Encode()
        {
            if (String.IsNullOrEmpty(InputFile)||!File.Exists(InputFile))
            {
                MessageBox.Show("Please select a valid input file to convert");
                return;
            }
            if (SelectedMediaType == null || SelectedMediaType.MediaType == null)
            {
                MessageBox.Show("Please select a valid output format");
                return;
            }

            using (var reader = new MediaFoundationReader(InputFile))
            {
                string outputUrl = SelectSaveFile(SelectedOutputFormat.Name, SelectedOutputFormat.Extension);
                if (outputUrl == null) return;
                using (var encoder = new MediaFoundationEncoder(SelectedMediaType.MediaType))
                {
                    encoder.Encode(outputUrl, reader);
                }
            }
        }

        private IMFSinkWriter CreateSinkWriter(string url)
        {
            var factory = (IMFReadWriteClassFactory)(new MFReadWriteClassFactory());

            // Create the attributes
            IMFAttributes attributes = MediaFoundationApi.CreateAttributes(1);
            
            attributes.SetUINT32(MediaFoundationAttributes.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);

            object sinkWriterObject;
            // TODO: need to move the class IDs into their own class for use in cases like this
            var guidMFSinkWriter = new Guid("a3bbfb17-8273-4e52-9e0e-9739dc887990");
            var guidIMFSinkWriter = new Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d");
            factory.CreateInstanceFromURL(guidMFSinkWriter, url, attributes, guidIMFSinkWriter, out sinkWriterObject);
            Marshal.ReleaseComObject(factory);
            return (IMFSinkWriter)sinkWriterObject;
        }

        private string SelectSaveFile(string formatName, string extension)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileNameWithoutExtension(InputFile) + " converted" + extension;
            sfd.Filter = formatName + "|*" + extension;
            //return (sfd.ShowDialog() == true) ? new Uri(sfd.FileName).AbsoluteUri : null;
            return (sfd.ShowDialog() == true) ? sfd.FileName : null;
        }

        public void Dispose()
        {
            MediaFoundationApi.Shutdown();
        }
    }

    enum AacPayloadType
    {
        /// <summary>
        /// The stream contains raw_data_block elements only.
        /// </summary>
        RawData = 0,
        /// <summary>
        /// Audio Data Transport Stream (ADTS). The stream contains an adts_sequence, as defined by MPEG-2.
        /// </summary>
        Adts = 1,
        /// <summary>
        /// Audio Data Interchange Format (ADIF). The stream contains an adif_sequence, as defined by MPEG-2.
        /// </summary>
        Adif = 2,
        /// <summary>
        /// The stream contains an MPEG-4 audio transport stream with a synchronization layer (LOAS) and a multiplex layer (LATM).
        /// </summary>
        LoasLatm = 3
    }

    internal class EncoderViewModel : ViewModelBase
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public string Extension { get; set; }
    }
}