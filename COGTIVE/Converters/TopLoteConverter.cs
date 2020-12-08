using COGTIVE.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace COGTIVE.Converters
{
    internal sealed class TopLoteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int index = System.Convert.ToInt32(parameter);
            return value is IEnumerable<Lote> tops && tops.ElementAtOrDefault(index) is Lote lote ?
                $"{(++index)}º Lote {lote.NumeroLote} Produziu {lote.QuantidadeProduzida:D6}" :
                $"{(++index)}º Lote Indefinido!";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
