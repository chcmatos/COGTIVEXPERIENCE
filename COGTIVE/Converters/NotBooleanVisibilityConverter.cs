using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal class NotBooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }
}
