using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoVFA.Parsers;

namespace AutoVFA.Models
{
    public class VFADataItem
    {
        private AnalysisInfo[] _analysisInfo;
        public bool Loaded { get; private set; }
        private Action<Exception> _resolveParseError;
        public string Name => Path.GetFileNameWithoutExtension(FileName);

        public IReadOnlyDictionary<string, object> Metadata { get; private set; }
        public bool UseMetadata { get; set; }

        public AnalysisInfo[] AnalysisInfo
        {
            get {
                EnsureDataLoaded();
                return _analysisInfo;
            }
            private set => _analysisInfo = value;
        }

        public string FileName { get; }

        public VFADataItem(string path)
        {
            FileName = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void EnsureDataLoaded()
        {
            if (!Loaded)
                LoadData();
        }

        public void LoadData()
        {
            try
            {
                var parser = new VFASummaryParser(FileName);
                if (UseMetadata)
                {
                    Metadata = parser.ParseFile();
                }
                var table = parser.ParseTable("Peak Info for Channel Front");
                AnalysisInfo = table.ToArray();
                Loaded = true;
            }
            catch (Exception e)
            {
                _resolveParseError?.Invoke(e);
            }
        }
         
        public VFADataItem Resolver(Action<string, Exception> onError)
        {
            _resolveParseError = e => onError(FileName, e);
            return this;
        }

        public void Clear()
        {
            Loaded = true;
            Metadata = default;
            AnalysisInfo = default;
        }
    }
}