using System;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class DimensionPercConverter : IValueConverter
    {
        private bool TryParseByCulture(out double res, string str, params string[] cultures)
        {
            if (str.Length > 0)
            {
                foreach (string cult in cultures)
                {
                    if (double.TryParse(str, NumberStyles.Float, new CultureInfo(cult), out res))
                    {
                        return true;
                    }
                }
            }

            res = default;
            return false;
        }

        private double ToDouble(object value, double defaultValue = 0d)
        {
            return value is null ? default :
                    value is string str ? (TryParseByCulture(out double res, str, "pt-BR", "en-US", "en-GB", "de-DE", "ja-JP") ? res : defaultValue) :
                    System.Convert.ToDouble(value);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double dValue = ToDouble(value);
            double pValue = Math.Max(Math.Min(ToDouble(parameter, 1d), 1d), 0d);
            return dValue * pValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
