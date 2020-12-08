using System;

namespace COGTIVE.Model
{
    internal class Apontamento
    {
        public long IDApontamento { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime DataFim { get; set; }

        public Lote Lote { get; set; }

        public int IDEvento { get; set; }

        public bool IsApontamentoProducao()
        {
            switch (this.IDEvento)
            {
                case 1:
                case 2:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsApontamentoManutencao()
        {
            return this.IDEvento == 19;
        }
    }
}
