using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(parameter is string format)
            {
                if(value is IEnumerable<object> args)
                {
                    return string.Format(format, args);
                } 
                else if(value is string str)
                {
                    return string.Format(format, str);
                }
                else
                {
                    return string.Format(format, (value ?? "???"));
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
