using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel.Composition;

namespace NAudioWpfDemo
{
    [Export(typeof(IModule))]
    class AudioPlaybackDemoPlugin : IModule
    {
        AudioPlaybackDemoView view;
        AudioPlaybackViewModel viewModel;

        public string Name
        {
            get { return "Audio Playback"; }
        }

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new AudioPlaybackDemoView();
            this.viewModel = new AudioPlaybackViewModel(view.waveForm, view.analyzer);
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            this.viewModel.Dispose();
            this.view = null;
        }
    }
}
