using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoVFA.Models;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

namespace AutoVFA.Misc
{
    internal class SampleAnalysisExporter : ExportContextBuilder
    {
        private readonly AnalyzingContext _context;
        private IEnumerable<ModelGroup> _modelGroups;

        private IAsyncEnumerable<(IList<AcidViewModel> model,
            Expression<Func<AcidSummary, double>> func)> _summary;

        private ValueThresholdConfig _vcThresholdConfig;

        public SampleAnalysisExporter(AnalyzingContext context)
        {
            _context = context;
            SetNormAcid(context.BaseNormAcid);
        }

        public SampleAnalysisExporter SetCVThreshold(ValueThresholdConfig value)
        {
            _vcThresholdConfig = value;
            return this;
        }

        public SampleAnalysisExporter SetSummary(
            IAsyncEnumerable<(IList<AcidViewModel> model,
                Expression<Func<AcidSummary, double>>)> summary)
        {
            _summary = summary;
            return this;
        }

        public override async Task ExportToXLSX(string fileName)
        {
            _vcThresholdConfig ??= new ValueThresholdConfig();
            try
            {
                var fi = new FileInfo(fileName);
                using var package = new ExcelPackage(fi);

                var offsetX = 2;
                var offsetY = 2;
                var wsName = "result";
                ExcelWorksheet worksheet =
                    package.Workbook.Worksheets.Any(x => x.Name == wsName)
                        ? package.Workbook.Worksheets[wsName]
                        : package.Workbook.Worksheets.Add(wsName);
                worksheet.Cells.Clear();
                await foreach (var (md, func) in _summary)
                {
                    var methodName = ((MemberExpression) func.Body).Member.Name;
                    ExcelRange methodNameCell =
                        worksheet.Cells[offsetX - 1, offsetY - 1];
                    methodNameCell.Style.Font.Bold = true;
                    methodNameCell.Value = methodName;
                    var commonModelKey =
                        md[0].Values.GetDynamicMemberNames().ToArray();
                    for (var i = 0; i < commonModelKey.Length; i++)
                        worksheet.Cells[offsetX - 1, offsetY + i].Value =
                            commonModelKey[i];

                    for (var index = 0; index < md.Count; index++)
                    {
                        AcidViewModel viewModel = md[index];
                        BindableDynamicDictionary dc = viewModel.Values;
                        var ks = dc.GetDynamicMemberNames().ToArray();
                        worksheet.Cells[offsetX + index, offsetY - 1].Value =
                            viewModel.Name;
                        for (var i = 0; i < ks.Length; i++)
                        {
                            var k = ks[i];
                            ExcelRange cell = worksheet.Cells[offsetX + index,
                                offsetY + i];
                            cell.Value = dc[k];
                            if (methodName.Contains("CV"))
                            {
                                var val = (double) dc[k];
                                if (val > _vcThresholdConfig.Danger)
                                    cell.Style.Fill.SetBackground(Color.Red);
                                else if (val > _vcThresholdConfig.Warning)
                                    cell.Style.Fill.SetBackground(Color.Orange);
                            }
                        }
                    }

                    offsetY = 2;
                    offsetX += _availableAcids.Length + 1; // table offset
                }

                worksheet.Cells.AutoFitColumns(0);
                worksheet.Calculate();
                await package.SaveAsync();
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }

        public SampleAnalysisExporter SetModelGroups(
            IEnumerable<ModelGroup> modelGroups)
        {
            _modelGroups = modelGroups;
            return this;
        }

        public override async Task ExportToCsv(string fileName)
        {
            var customizedCulture =
                (CultureInfo) CultureInfo.CurrentCulture.Clone();
            customizedCulture.NumberFormat.NumberDecimalSeparator =
                _context.CsvDecimalSeparator;
            var cfg = new CsvConfiguration(customizedCulture)
                {Delimiter = _context.CsvDelimiter};

            cfg.TypeConverterOptionsCache.GetOptions<double>().Formats =
                new[] {$"F{_context.DecimalDigits}"};
            if (File.Exists(fileName)) File.Delete(fileName);
            await using FileStream file = File.OpenWrite(fileName);
            await using var streamWriter = new StreamWriter(file);
            await using var csvWriter = new CsvWriter(streamWriter, cfg);

            var dc = new List<IDictionary<string, object>>();

            foreach (ModelGroup model in _modelGroups)
            {
                dynamic exp = new ExpandoObject();
                var u = exp as IDictionary<string, object>;
                u["Model"] = model.Name;

                foreach (var acid in _availableAcids
                    .Except(new[] {_baseNormAcid}))
                {
                    u[$"Concentration {acid}"] = model.Avg(acid);
                    u[$"% {acid}"] = model.CVFraction(acid);
                    u[$"CV {acid}"] = model.CVmM(acid);
                }

                dc.Add(u);
            }

            await csvWriter.WriteRecordsAsync(
                dc.Select(x => (dynamic) (ExpandoObject) x));

            await csvWriter.FlushAsync();
        }
    }
}