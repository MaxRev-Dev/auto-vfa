using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoVFA.Models;

namespace AutoVFA.Misc
{
    internal abstract class ExportContextBuilder : IExportContextBuilder
    {
        protected string[] _availableAcids;
        protected string _baseNormAcid;
        protected Action<Exception> _onError;
        protected IList<RegressionResult> _results;
        protected IList<VFADataItem> _standards;

        public IExportContextBuilder ErrorResolver(Action<Exception> onError)
        {
            _onError = onError;
            return this;
        }

        public abstract Task ExportToXLSX(string fileName);

        public abstract Task ExportToCsv(string fileName);

        public IExportContextBuilder SetNormAcid(string norm)
        {
            _baseNormAcid = norm;
            return this;
        }

        public IExportContextBuilder SetAvailableAcids(
            IEnumerable<string> names)
        {
            _availableAcids = names.ToArray();
            return this;
        }

        public IExportContextBuilder SetRegressionResults(
            IList<RegressionResult> results)
        {
            _results = results;
            return this;
        }

        public IExportContextBuilder SetStandards(IList<VFADataItem> standards)
        {
            _standards = standards;
            return this;
        }
    }
}