using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal class Int32VisibilityConverter : IValueConverter
    {
        private int ToInt32(object value)
        {
            return value is null || (value is string str && str.Length == 0) ? -1 : System.Convert.ToInt32(value);
        }

        protected virtual bool Compare(int valueInt, int paramInt)
        {
            return valueInt == paramInt;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int valueInt = ToInt32(value);
            int paramInt = ToInt32(parameter);
            return Compare(valueInt, paramInt) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
