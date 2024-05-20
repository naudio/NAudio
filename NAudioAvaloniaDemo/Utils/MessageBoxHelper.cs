using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace NAudioAvaloniaDemo.Utils
{
    public static class MessageBoxExtensions
    {
        public static async Task<ButtonResult> ShowMessageBoxAsync(this Window window, string messageBoxText, string caption = "", ButtonEnum button = ButtonEnum.Ok, Icon icon = Icon.None, double maxWidth = 500)
        {
            if (!window.IsVisible)
                return await MessageBox.ShowAsync(messageBoxText, caption, button, icon, maxWidth);

            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams()
            {
                ContentMessage = messageBoxText,
                ContentTitle = caption,
                ButtonDefinitions = button,
                Icon = icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MaxWidth = maxWidth,
            });
            return await box.ShowWindowDialogAsync(window);
        }
    }

    public static class MessageBox
    {
        public static async Task<ButtonResult> ShowAsync(string messageBoxText, string caption = "", ButtonEnum button = ButtonEnum.Ok, Icon icon = Icon.None, double maxWidth = 500)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams()
            {
                ContentMessage = messageBoxText,
                ContentTitle = caption,
                ButtonDefinitions = button,
                Icon = icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MaxWidth = maxWidth,
            });
            return await box.ShowWindowDialogAsync(App.MainWindow);
        }
    }
}