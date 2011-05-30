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
    class AudioPlaybackDemoPlugin : IModule
    {
        private AudioPlaybackDemoView view;
        private AudioPlaybackViewModel viewModel;
        private IEnumerable<IVisualizationPlugin> visualizations;

        [ImportingConstructor]
        public AudioPlaybackDemoPlugin([ImportMany(typeof(IVisualizationPlugin))] IEnumerable<IVisualizationPlugin> visualizations)
        {
            this.visualizations = visualizations;
        }

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
            this.viewModel = new AudioPlaybackViewModel(visualizations);
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            this.viewModel.Dispose();
            this.view = null;
        }
    }
}
