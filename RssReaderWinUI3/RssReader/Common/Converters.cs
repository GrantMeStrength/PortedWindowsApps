// MIGRATION NOTE:
// BooleanToVisibilityConverter — still needed in WinUI 3.
// There is NO built-in one (unlike WPF). You can also use 
// CommunityToolkit.WinUI.Converters.BoolToVisibilityConverter.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RssReader.Common;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}

public class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is bool b ? !b : value;
}
