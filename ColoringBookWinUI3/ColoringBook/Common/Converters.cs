using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ColoringBook.Common
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool boolValue = value is bool b && b;
            if (invert) boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
