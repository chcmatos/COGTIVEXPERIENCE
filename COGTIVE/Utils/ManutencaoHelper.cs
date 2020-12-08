using COGTIVE.Model;

namespace COGTIVE.Utils
{
    internal static class ManutencaoHelper
    {
        public static Manutencao CalcularManutencao(Manutencao m, Apontamento curr, int _)
        {
            if(curr.IsApontamentoManutencao())
            {
                m = m ?? new Manutencao();
                m.PeriodoTotal += curr.DataFim - curr.DataInicio;
            }

            return m;
        }
    }
}
