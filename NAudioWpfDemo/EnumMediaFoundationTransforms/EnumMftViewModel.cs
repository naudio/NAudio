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
            foreach(var mft in MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect))
            {
                int attributeCount;
                mft.GetCount(out attributeCount);
                var sb = new StringBuilder();
                for (int n = 0; n < attributeCount; n++)
                {
                    Guid key;
                    var value = new PropVariant();
                    mft.GetItemByIndex(n, out key, ref value);
                    if ((value.DataType & VarEnum.VT_VECTOR) == VarEnum.VT_VECTOR)
                    {
                        sb.AppendFormat("{0}: Vector of {1}", key, value.DataType);
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1}", key, value.Value);
                    }
                    
                    sb.AppendLine();
                    value.Clear();
                }
                Transforms.Add(sb.ToString());
            }
            OnPropertyChanged("Transforms");
        }

        public void Dispose()
        {
            MediaFoundationApi.Shutdown();
        }
    }
}