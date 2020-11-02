using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoVFA.Parsers;

namespace AutoVFA.Models
{
    public class VFADataItem
    {
        private Action<Exception> _resolveParseError;
        private volatile bool _loaded;
        private readonly object _gate = new object();

        public VFADataItem(string path)
        {
            FileName = path ?? throw new ArgumentNullException(nameof(path));
        }

        public bool Loaded
        {
            get => _loaded;
            private set => _loaded = value;
        }

        public string Name => Path.GetFileNameWithoutExtension(FileName);

        public IReadOnlyDictionary<string, object> Metadata
        {
            get;
            private set;
        }

        public bool UseMetadata { get; set; }

        public AnalysisInfo[] AnalysisInfo { get; private set; }

        public string FileName { get; }

        public Task<bool> EnsureDataLoadedAsync()
        {
            lock (_gate)
                if (!_loaded)
                    Task.Run(LoadData).GetAwaiter().GetResult();
            return Task.FromResult(_loaded);
        }
         
        public async Task LoadData()
        {
            await Task.Run(() =>
            {
                try
                {
                    var parser = new VFASummaryParser(FileName);
                    if (UseMetadata) Metadata = parser.ParseFile();
                    var table = parser.ParseTable("Peak Info for Channel Front");
                    AnalysisInfo = table.ToArray();
                    _loaded = true;
                }
                catch (Exception e)
                {
                    _resolveParseError?.Invoke(e);
                }
            });
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