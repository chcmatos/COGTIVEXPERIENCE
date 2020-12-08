using System;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class NotBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && !b;
        }
    }
}
