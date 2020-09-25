using System;

namespace FortniteReplayAnalyzer.Data
{
    public class FortniteGameAnalysis
    {
        public string AnalysisUrl { get; internal set; }
        public string RawData { get; internal set; }

        public Boolean Successful { get; internal set; }
        public string Message { get; internal set; }
        public FortniteGame Game { get; internal set; }
    }
}
