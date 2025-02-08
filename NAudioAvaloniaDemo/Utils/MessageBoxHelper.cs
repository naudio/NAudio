using System.Threading.Tasks;
using DialogHostAvalonia;
using NAudioAvaloniaDemo.Views;

namespace NAudioAvaloniaDemo.Utils
{
    public static class MessageBox
    {
        public static async Task ShowAsync(string messageBoxText, double maxWidth = 500, double maxHeight = 250)
        {
            await DialogHost.Show(new PopupView() 
            {
                DataContext = new PopupViewModel 
                { 
                    Message = messageBoxText,
                    MaxWidth = maxWidth, 
                    MaxHeight = maxHeight 
                } 
            });
        }
    }
}