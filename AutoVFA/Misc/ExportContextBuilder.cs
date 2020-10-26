using System;
using System.Collections.Generic;
using System.Linq;
using AutoVFA.Models;

namespace AutoVFA.Misc
{
    internal abstract class ExportContextBuilder : IExportContextBuilder
    {
        protected string[] _availableAcids;
        protected IList<RegressionResult> _results;
        protected IList<VFADataItem> _standards;
        protected string _baseNormAcid;
        protected Action<Exception> _onError;

        public IExportContextBuilder ErrorResolver(Action<Exception> onError)
        {
            _onError = onError;
            return this;
        }

        public abstract void ExportToXLSX(string fileName);

        public IExportContextBuilder SetNormAcid(string norm)
        {
            _baseNormAcid = norm;
            return this;
        }

        public IExportContextBuilder SetAvailableAcids(IEnumerable<string> names)
        {
            _availableAcids = names.ToArray();
            return this;
        }

        public IExportContextBuilder SetRegressionResults(IList<RegressionResult> results)
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