using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using NAudioWpfDemo.AudioPlaybackDemo;

namespace NAudioWpfDemo
{
    [Export(typeof(IModule))]
    class WasapiCaptureDemoPlugin : IModule
    {
        private WasapiCaptureViewModel viewModel;
        private WasapiCaptureDemoView view;

        public WasapiCaptureDemoPlugin()
        {

        }

        public string Name
        {
            get { return "WASAPI Capture"; }
        }

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new WasapiCaptureDemoView();
            this.viewModel = new WasapiCaptureViewModel();
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            this.viewModel.Dispose();
            this.view = null;
        }
    }
}
