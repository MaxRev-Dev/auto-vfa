using System.Collections.Generic;

namespace AutoVFA.Parsers
{
    public interface ISummaryParser
    {
        IEnumerable<AnalysisInfo> ParseTable(string name);
        Dictionary<string, object> ParseFile();
    }
}