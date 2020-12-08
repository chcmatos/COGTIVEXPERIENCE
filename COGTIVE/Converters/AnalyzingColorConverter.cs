using COGTIVE.Enums;
using COGTIVE.Utils;
using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace COGTIVE.Converters
{
    internal sealed class AnalyzingColorConverter : IValueConverter
    {
        private Brush GetResourceColor(string name)
        {
            object res = ResourceHelper.GetResourceColor(name);
            return res is Brush b ? b : res is Color c ? new SolidColorBrush(c) : null;
        }

        private AnalyzingStates GetAnalyzingState(object value)
        {
            return value is null ? throw new NullReferenceException() :
                value is AnalyzingStates s ? s :
                value is int i ? (AnalyzingStates)i :
                (AnalyzingStates)Enum.Parse(typeof(AnalyzingStates), value.ToString());
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (GetAnalyzingState(value))
            {
                case AnalyzingStates.Analyzing:
                case AnalyzingStates.Sleeping:
                    return GetResourceColor("SystemChromeWhiteColor");
                case AnalyzingStates.Done:
                    return GetResourceColor("Accent");
                case AnalyzingStates.Error:
                    return GetResourceColor("Error");
                default:
                    throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
