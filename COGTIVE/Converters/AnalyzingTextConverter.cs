using COGTIVE.Utils;
using System;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class AnalyzingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is AnalyzingText text ? text.ToString() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
