using System;
using System.Collections.Generic;
using AutoVFA.Models;

namespace AutoVFA.Misc
{
    internal interface IExportContextBuilder
    {
        IExportContextBuilder SetNormAcid(string norm);
        IExportContextBuilder SetAvialableAcids(IEnumerable<string> names);
        IExportContextBuilder SetRegressionResults(IList<RegressionResult> results);
        IExportContextBuilder SetStandards(IList<VFADataItem> standards);
        IExportContextBuilder ErrorResolver(Action<Exception> onError);
        void ExportToXLSX(string fileName);
    }
}