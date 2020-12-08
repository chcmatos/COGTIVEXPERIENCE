using System;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class AnalyzingResultTitleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is IStorageItem item)
            {
                return $"Resultado gerado a apartir da análise do arquivo de\r\napontamentos \"{item.Path}\".\r\n" +
                    $"Confira abaixo os resultados obtidos.";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
