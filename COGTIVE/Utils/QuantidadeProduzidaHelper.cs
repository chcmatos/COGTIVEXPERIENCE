using COGTIVE.Model;
using System;
using System.Diagnostics;

namespace COGTIVE.Utils
{
    internal static class QuantidadeProduzidaHelper
    {
        public static QuantidadeProduzida CalcularQuantidadeProduzida(QuantidadeProduzida prod, Apontamento curr, int index)
        {
            if(curr.IsApontamentoProducao())
            {
                prod = prod ?? new QuantidadeProduzida();
                Lote currLote = curr.Lote;
                Debug.Assert(currLote.QuantidadeProduzida >= 0, $"Ops! O apontamento {curr.IDApontamento} no indice {index} " +
                    "é do tipo produção, mas não possui Quantidade válida!");

                Debug.Assert(!string.IsNullOrEmpty(currLote.NumeroLote), $"Ops! O apontamento {curr.IDApontamento} no indice {index} " +
                    "é do tipo produção, mas não possui NumeroLote válido!");

                if(prod.Lotes.TryGetValue(curr.Lote, out Lote actual))
                {
                    actual.QuantidadeProduzida += currLote.QuantidadeProduzida;                    
                } 
                else if(!prod.Lotes.Add(curr.Lote))                
                {
                    #if DEBUG
                    throw new InvalidOperationException("Não foi possível add lote para calculo de quantidade produzida!");
                    #endif
                }

                prod.Total += currLote.QuantidadeProduzida;
            }

            return prod;
        }
    }
}
