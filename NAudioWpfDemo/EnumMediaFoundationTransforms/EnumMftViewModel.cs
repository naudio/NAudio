using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EnumMediaFoundationTransforms
{
    class EnumMftViewModel : ViewModelBase, IDisposable
    {
        public EnumMftViewModel()
        {
            MediaFoundationApi.Startup();
            this.EnumerateCommand = new DelegateCommand(Enumerate);
        }

        public ICommand EnumerateCommand { get; private set; }

        public List<string> Transforms { get; private set; }

        private void Enumerate()
        {
            Transforms = new List<string>();

            var effects = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect);
            AddTransforms(effects, "Audio Effect");
            AddTransforms(MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioDecoder), "Audio Decoder");
            AddTransforms(MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEncoder), "Audio Encoder");
            OnPropertyChanged("Transforms");
        }

        private Dictionary<Guid, string> attributeDescriptions = new Dictionary<Guid, string>()
        {
            { new Guid("314ffbae-5b41-4c95-9c19-4e7d586face3"), "MFT_FRIENDLY_NAME_Attribute"},
            { new Guid("4276c9b1-759d-4bf3-9cd0-0d723d138f96"), "MFT_INPUT_TYPES_Attributes"},
            { new Guid("8eae8cf3-a44f-4306-ba5c-bf5dda242818"), "MFT_OUTPUT_TYPES_Attributes"},
            { new Guid("6821c42b-65a4-4e82-99bc-9a88205ecd0c"), "MFT_TRANSFORM_CLSID_Attribute"},
            { new Guid("9359bb7e-6275-46c4-a025-1c01e45f1a86"), "MF_TRANSFORM_FLAGS_Attribute"},
            { new Guid("ceabba49-506d-4757-a6ff-66c184987e4e"), "MF_TRANSFORM_CATEGORY_Attribute"},
        };
        
        private void AddTransforms(IEnumerable<IMFActivate> effects, string type)
        {
            foreach (var mft in effects)
            {
                int attributeCount;
                mft.GetCount(out attributeCount);
                var sb = new StringBuilder();
                sb.AppendFormat(type);
                sb.AppendLine();
                for (int n = 0; n < attributeCount; n++)
                {
                    Guid key;
                    var value = new PropVariant();
                    mft.GetItemByIndex(n, out key, ref value);
                    string propertyName;
                    if (!attributeDescriptions.TryGetValue(key, out propertyName))
                        propertyName = key.ToString();
                    if (value.DataType == (VarEnum.VT_VECTOR | VarEnum.VT_UI1))
                    {
                        var b = (byte[])value.Value;
                        sb.AppendFormat("{0}: Blob of {1} bytes", propertyName, b.Length);
                    }
                    else
                    {
                        sb.AppendFormat("{0}: {1}", propertyName, value.Value);
                    }


                    sb.AppendLine();
                    value.Clear();
                }
                Transforms.Add(sb.ToString());
            }
        }

        public void Dispose()
        {
            MediaFoundationApi.Shutdown();
        }
    }
}