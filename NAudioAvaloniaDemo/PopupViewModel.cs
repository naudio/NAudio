using NAudioAvaloniaDemo.ViewModel;

namespace NAudioAvaloniaDemo
{
    class PopupViewModel : ViewModelBase
    {
        private double maxWidth;
        private double maxHeight;
        private string message;

        public double MaxWidth
        {
            get => maxWidth;
            set => SetProperty(ref maxWidth, value);
        }

        public double MaxHeight
        {
            get => maxHeight;
            set => SetProperty(ref maxHeight, value);
        }

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }
    }
}
