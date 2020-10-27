using AutoVFA.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace AutoVFA.Misc
{
    internal class SampleAnalysisExporter : ExportContextBuilder
    {
        private IEnumerable<(IList<AcidViewModel> model, Expression<Func<AcidSummary, double>> func)> _summary;
        private CVThreshold _vcThreshold;

        public SampleAnalysisExporter SetCVThreshold(CVThreshold value)
        {
            _vcThreshold = value;
            return this;
        }

        public override void ExportToXLSX(string fileName)
        {
            _vcThreshold ??= new CVThreshold();
            try
            {
                var fi = new FileInfo(fileName);
                using var package = new ExcelPackage(fi);

                var offsetX = 2;
                var offsetY = 2;
                var wsName = "result";
                var worksheet = package.Workbook.Worksheets.Any(x => x.Name == wsName) ?
                    package.Workbook.Worksheets[wsName] :
                    package.Workbook.Worksheets.Add(wsName);
                worksheet.Cells.Clear();
                foreach (var (md, func) in _summary)
                {
                    var methodName = ((MemberExpression)func.Body).Member.Name;
                    var methodNameCell = worksheet.Cells[offsetX - 1, offsetY - 1];
                    methodNameCell.Style.Font.Bold = true;
                    methodNameCell.Value = methodName;
                    var commonModelKey = md[0].Values.GetDynamicMemberNames().ToArray();
                    for (int i = 0; i < commonModelKey.Length; i++)
                    {
                        worksheet.Cells[offsetX - 1, offsetY + i].Value = commonModelKey[i];
                    }

                    for (var index = 0; index < md.Count; index++)
                    {
                        var viewModel = md[index];
                        var dc = viewModel.Values;
                        var ks = dc.GetDynamicMemberNames().ToArray();
                        worksheet.Cells[offsetX + index, offsetY - 1].Value = viewModel.Name;
                        for (var i = 0; i < ks.Length; i++)
                        {
                            var k = ks[i];
                            var cell = worksheet.Cells[offsetX + index, offsetY + i];
                            cell.Value = dc[k];
                            if (methodName.Contains("CV"))
                            {
                                var val = (double)dc[k];
                                if (val > _vcThreshold.Danger)
                                    cell.Style.Fill.SetBackground(Color.Red);
                                else if (val > _vcThreshold.Warning)
                                    cell.Style.Fill.SetBackground(Color.Orange);

                            }
                        }
                    }

                    offsetY = 2;
                    offsetX += _availableAcids.Length + 1; // table offset
                }

                worksheet.Cells.AutoFitColumns(0);
                worksheet.Calculate();
                package.Save();
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }

        public IExportContextBuilder SetSummary(IEnumerable<(IList<AcidViewModel> model,
            Expression<Func<AcidSummary, double>> func)> summary)
        {
            _summary = summary;
            return this;
        }
    }
}