using System;

namespace COGTIVE.Model
{
    internal class Apontamento
    {
        public long IDApontamento { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime DataFim { get; set; }

        public string NumerLote { get; set; }

        public int IDEvento { get; set; }

        public int Quantidade { get; set; }
    }
}
