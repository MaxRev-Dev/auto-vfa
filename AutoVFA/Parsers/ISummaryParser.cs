using System.Collections.Generic;
using AutoVFA.Models;

namespace AutoVFA.Parsers
{
    public interface ISummaryParser
    {
        IEnumerable<AnalysisInfo> ParseTable(string name);
        Dictionary<string, object> ParseFile();
    }
}