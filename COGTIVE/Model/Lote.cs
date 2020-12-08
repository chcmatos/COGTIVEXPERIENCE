using System;

namespace COGTIVE.Model
{
    internal class Lote : IComparable<Lote>
    {
        private readonly int hashCode;

        public string NumeroLote { get; }

        public int QuantidadeProduzida { get; set; }

        public Lote(string numeroLote, int quantidade)
        {
            this.NumeroLote = numeroLote;
            this.QuantidadeProduzida = quantidade;
            this.hashCode = GetHashCode(numeroLote);
        }

        private int GetHashCode(string numeroLote)
        {
            return string.IsNullOrEmpty(numeroLote) ? base.GetHashCode() :
                int.TryParse(numeroLote, out int res) ? res : numeroLote.GetHashCode(); 
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return this == obj || (obj is Lote other && this.CompareTo(other) == 0);
        }

        public int CompareTo(Lote other)
        {
            return NumeroLote == null ? -1 : 
                other.NumeroLote == null ? 1 :
                this.NumeroLote.CompareTo(other.NumeroLote);
        }
    }
}
