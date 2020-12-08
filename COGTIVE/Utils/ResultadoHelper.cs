using COGTIVE.Model;

namespace COGTIVE.Utils
{
    internal static class ResultadoHelper
    {
        public static Resultado CalcularResultado(Resultado res, Apontamento curr, int index)
        {
            res.Gaps = GapsHelper.CalcularGap(res.Gaps, curr, index);
            res.QuantidadeProduzida = QuantidadeProduzidaHelper.CalcularQuantidadeProduzida(res.QuantidadeProduzida, curr, index);
            res.Manutencao = ManutencaoHelper.CalcularManutencao(res.Manutencao, curr, index);
            return res;
        }
    }
}
