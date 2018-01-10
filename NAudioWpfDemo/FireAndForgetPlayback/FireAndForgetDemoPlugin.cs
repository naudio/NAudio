namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class FireAndForgetPlaybackDemoPlugin : IModule
    {
        private FireAndForgetPlaybackView view;
        private FireAndForgetPlaybackViewModel viewModel;

        public string Name => "Fire and Forget";

        public System.Windows.Controls.UserControl UserInterface
        {
            get 
            {
                if (view == null)
                {
                    view = new FireAndForgetPlaybackView();
                    viewModel = new FireAndForgetPlaybackViewModel();
                    view.DataContext = viewModel;
                }
                return view;
            }
        }

        public void Deactivate()
        {
            if (view != null)
            {
                viewModel.Dispose();
                view = null;
                viewModel = null;
            }
        }
    }
}
