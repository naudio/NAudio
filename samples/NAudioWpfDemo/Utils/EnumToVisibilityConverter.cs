using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NAudioWpfDemo.Utils;

/// <summary>
/// Shows an element only while the bound enum equals the value passed as the converter
/// parameter, and collapses it otherwise. Used to hide options that don't apply to the
/// currently selected mode.
/// </summary>
internal class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Equals(value, parameter) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
