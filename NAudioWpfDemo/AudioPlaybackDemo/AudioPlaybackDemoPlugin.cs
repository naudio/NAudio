using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using NAudioWpfDemo.Utils;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    class AudioPlaybackDemoPlugin : IModule
    {
        private AudioPlaybackDemoView view;
        private AudioPlaybackViewModel viewModel;
        private readonly IEnumerable<IVisualizationPlugin> visualizations;

        public AudioPlaybackDemoPlugin() : this(ReflectionHelper.CreateAllInstancesOf<IVisualizationPlugin>())
        {
            
        }

        public AudioPlaybackDemoPlugin(IEnumerable<IVisualizationPlugin> visualizations)
        {
            this.visualizations = visualizations;
        }

        public string Name => "Audio Playback";

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new AudioPlaybackDemoView();
            viewModel = new AudioPlaybackViewModel(visualizations);
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            viewModel.Dispose();
            view = null;
        }
    }
}
