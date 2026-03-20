using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EnumMediaFoundationTransforms
{
    class EnumMftViewModel : ViewModelBase, IDisposable
    {
        public EnumMftViewModel()
        {
            MediaFoundationApi.Startup();
            EnumerateCommand = new DelegateCommand(Enumerate);
        }

        public ICommand EnumerateCommand { get; }

        public List<string> Transforms { get; private set; }

        private void Enumerate()
        {
            Transforms = new List<string>();

            AddTransforms(MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect), "Audio Effect");
            AddTransforms(MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioDecoder), "Audio Decoder");
            AddTransforms(MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEncoder), "Audio Encoder");
            OnPropertyChanged("Transforms");
        }

        private void AddTransforms(IEnumerable<MfActivate> transforms, string type)
        {
            foreach (var mft in transforms)
            {
                Transforms.Add(DescribeMft(type, mft));
            }
        }

        private string DescribeMft(string type, MfActivate mft)
        {
            int attributeCount = mft.AttributeCount;
            var sb = new StringBuilder();
            sb.AppendLine(type);
            for (int n = 0; n < attributeCount; n++)
            {
                AddAttribute(mft, n, sb);
            }
            return sb.ToString();
        }

        private static void AddAttribute(MfActivate mft, int index, StringBuilder sb)
        {
            var variantPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
            try
            {
                mft.GetAttributeByIndex(index, out var key, variantPtr);
                var value = Marshal.PtrToStructure<PropVariant>(variantPtr);
                var propertyName = FieldDescriptionHelper.Describe(typeof(MediaFoundationAttributes), key);
                if (key == MediaFoundationAttributes.MFT_INPUT_TYPES_Attributes ||
                    key == MediaFoundationAttributes.MFT_OUTPUT_TYPES_Attributes)
                {
                    var types = mft.GetBlobAsArrayOf<MftRegisterTypeInfo>(key);
                    sb.AppendFormat("{0}: {1} items:", propertyName, types.Length);
                    sb.AppendLine();
                    foreach (var t in types)
                    {
                        sb.AppendFormat("    {0}-{1}",
                            FieldDescriptionHelper.Describe(typeof(MediaTypes), t.MajorType),
                            FieldDescriptionHelper.Describe(typeof(AudioSubtypes), t.SubType));
                        sb.AppendLine();
                    }
                }
                else if (key == MediaFoundationAttributes.MF_TRANSFORM_CATEGORY_Attribute)
                {
                    sb.AppendFormat("{0}: {1}", propertyName,
                        FieldDescriptionHelper.Describe(typeof(MediaFoundationTransformCategories), (Guid)value.Value));
                    sb.AppendLine();
                }
                else if (value.DataType == (VarEnum.VT_VECTOR | VarEnum.VT_UI1))
                {
                    var b = (byte[])value.Value;
                    sb.AppendFormat("{0}: Blob of {1} bytes", propertyName, b.Length);
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendFormat("{0}: {1}", propertyName, value.Value);
                    sb.AppendLine();
                }
            }
            finally
            {
                PropVariant.Clear(variantPtr);
                Marshal.FreeHGlobal(variantPtr);
            }
        }

        public void Dispose()
        {
            MediaFoundationApi.Shutdown();
        }
    }
}
