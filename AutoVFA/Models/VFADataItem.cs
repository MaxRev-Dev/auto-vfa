using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoVFA.Parsers;

namespace AutoVFA.Models
{
    public class VFADataItem
    {
        private AnalysisInfo[] _analysisInfo;
        public string Name => Path.GetFileNameWithoutExtension(FileName);

        public IReadOnlyDictionary<string, object> Metadata { get; private set; }

        public AnalysisInfo[] AnalysisInfo
        {
            get
            {
                EnsureDataLoaded();
                return _analysisInfo;
            }
            private set => _analysisInfo = value;
        }

        public string FileName { get; }

        public VFADataItem(string path)
        {
            FileName = path;
        }

        public void EnsureDataLoaded()
        {
            if (Metadata == default)
                LoadData();
        }

        public void LoadData()
        {
            var parser = new VFASummaryParser(FileName);
            var result = parser.ParseFile();
            var table = parser.ParseTable("Peak Info for Channel Front");
            Metadata = result;
            AnalysisInfo = table.ToArray();
        }

        public void Clear()
        {
            Metadata = default;
            AnalysisInfo = default;
        } 
    }
}