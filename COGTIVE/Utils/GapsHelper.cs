using COGTIVE.Model;
using System;
using System.Diagnostics;

namespace COGTIVE.Utils
{
    internal static class GapsHelper
    {
        public static Gaps CalcularGap(Gaps gaps, Apontamento curr, int _)
        {
            if(gaps == null)
            {
                return new Gaps { UltimoAvaliado = curr, TotalApontamentos = 1 };
            }

            Debug.Assert(gaps.UltimoAvaliado.DataFim <= curr.DataInicio, "Ops! Algo deu errado, os apontametos estão fora de ordem!");

            TimeSpan diff = curr.DataInicio - gaps.UltimoAvaliado.DataFim;

            if(diff.TotalSeconds > 0d)
            {
                gaps.QuantidadeGaps++;
                gaps.TotalDuracaoGaps += diff;
            }

            gaps.TotalApontamentos++;
            gaps.UltimoAvaliado = curr;
            return gaps;
        }
    }
}
