using System.Collections.Generic;
using System.Linq;

namespace COGTIVE.Model
{
    internal class QuantidadeProduzida
    {
        public long Total { get; set; }

        public HashSet<Lote> Lotes { get; set; }

        public List<Lote> Tops
        {
            get;
            private set;
        }

        public QuantidadeProduzida()
        {
            Lotes = new HashSet<Lote>();
        }

        public void UpdateTopList()
        {
           Tops = Lotes.OrderByDescending(e => e.QuantidadeProduzida).Take(3).ToList();
        }

    }
}
