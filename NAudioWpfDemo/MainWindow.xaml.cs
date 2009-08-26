using System.Windows;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ControlPanelViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            viewModel = new ControlPanelViewModel(this.waveForm, this.analyzer);
            this.controlPanel.DataContext = viewModel;
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            viewModel.Dispose();
        }


    }
}
