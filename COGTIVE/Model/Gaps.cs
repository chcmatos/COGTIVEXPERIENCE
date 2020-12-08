using System;

namespace COGTIVE.Model
{
    /// <summary>
    /// Gaps entre o primeiro e último apontamento analisado.
    /// </summary>
    internal class Gaps
    {
        /// <summary>
        /// Quantidade total de gaps (intervalos ocioso entre apontamentos).
        /// </summary>
        public int QuantidadeGaps { get; set; }

        /// <summary>
        /// Total de apontamentos avaliados.
        /// </summary>
        public int TotalApontamentos { get; set; }

        /// <summary>
        /// período total de duração de gaps (soma da duração de todos os gaps encontrados no formato horas:minutos:segundos).
        /// </summary>
        public TimeSpan TotalDuracaoGaps { get; set; }

        /// <summary>
        /// Utilizado para calculo de gaps.
        /// Ultimo apontamento observado.
        /// </summary>
        public Apontamento UltimoAvaliado { get; set; }
    }
}
