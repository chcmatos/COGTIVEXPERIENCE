using COGTIVE.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace COGTIVE.Utils
{
    internal static class ApontamentoHelper
    {
        private static DateTime ToDateTime(string str)
        {
            return DateTime.ParseExact(str, "dd/MM/yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("pt-BR"));
        }

        public static Apontamento FromEntry(IReadOnlyList<string> entry)
        {
            return new Apontamento
            {
                IDApontamento = long.Parse(entry.ElementAtOrDefault(0)),
                DataInicio = ToDateTime(entry.ElementAtOrDefault(1)),
                DataFim = ToDateTime(entry.ElementAtOrDefault(2)),
                Lote = new Lote(entry.ElementAtOrDefault(3), int.Parse(entry.ElementAtOrDefault(5))),
                IDEvento = int.Parse(entry.ElementAtOrDefault(4))
            };
        }
    }
}
