using System;

namespace COGTIVE.Events
{
    internal class AnalyzerProgressEventArgs : EventArgs
    {
        public long TotalSize { get; }

        public long Consumed { get; }

        public long Percentage { get; }

        public AnalyzerProgressEventArgs(long totalSize, long consumed)
        {
            this.TotalSize  = totalSize;
            this.Consumed   = consumed;
            this.Percentage = Convert.ToInt64(((double)consumed / totalSize) * 100);
        }

    }
}
