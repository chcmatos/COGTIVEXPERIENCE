using COGTIVE.Enums;
using System;
using Windows.Storage;

namespace COGTIVE.Utils
{
    internal sealed class AnalyzingText
    {
        public AnalyzingStates State { get; }

        public IStorageItem File { get; }

        public Exception Error { get; }

        public AnalyzingText(AnalyzingStates state, IStorageItem file = null, Exception error = null)
        {
            this.State = state;
            this.File = file;
            this.Error = error;
        }

        public override string ToString()
        {
            switch (this.State)
            {
                case AnalyzingStates.Analyzing when File != null:
                    return $"Aguarde, analisando dados em \"{File.Path}\"...";
                case AnalyzingStates.Error when Error != null:
                    return $"Erro ao tentar analisar dados:\r\n{Error.Message}";
                case AnalyzingStates.Error:
                    return "Ocorreu um erro ao tentar analisar dados!";
                case AnalyzingStates.Done:
                    return "Arquivo analisado com sucesso.\r\nConfira os resultados ao lado";
                case AnalyzingStates.Sleeping:
                default:
                    return "...";
            }
        }
    }
}
