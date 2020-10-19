using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoVFA.Parsers;

namespace AutoVFA.Models
{
    public class VFADataItem
    {
        public string Name => Path.GetFileNameWithoutExtension(_path);
        private readonly string _path;
        public Dictionary<string, object> Metadata { get; private set; }
        public AnalysisInfo[] AnalysisInfo { get; private set; }

        public VFADataItem(string path)
        {
            _path = path;
        }

        public void EnsureDataLoaded()
        {
            if (Metadata == default && AnalysisInfo == default)
                LoadData();
        }

        public void LoadData()
        {
            var parser = new VFASummaryParser(_path);
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