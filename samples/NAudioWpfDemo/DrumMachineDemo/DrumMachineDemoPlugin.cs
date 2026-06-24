namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoPlugin : IModule
    {
        private DrumMachineDemoView view;
        private DrumMachineDemoViewModel viewModel;

        public string Name => "Drum Machine";

        public System.Windows.Controls.UserControl UserInterface
        {
            get 
            {
                if (view == null)
                {
                    view = new DrumMachineDemoView();
                    viewModel = new DrumMachineDemoViewModel(view.drumPatternEditor1.DrumPattern);
                    view.DataContext = viewModel;
                }
                return view;
            }
        }

        public void Deactivate()
        {
            viewModel?.Dispose();
            view = null;
            viewModel = null;
        }
    }
}
