using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoVFA.Models;

namespace AutoVFA.Misc
{
    internal interface IExportContextBuilder
    {
        IExportContextBuilder SetNormAcid(string norm);
        IExportContextBuilder SetAvailableAcids(IEnumerable<string> names);

        IExportContextBuilder SetRegressionResults(
            IList<RegressionResult> results);

        IExportContextBuilder SetStandards(IList<VFADataItem> standards);
        IExportContextBuilder ErrorResolver(Action<Exception> onError);
        Task ExportToXLSX(string fileName);
        Task ExportToCsv(string fileName);
    }
}