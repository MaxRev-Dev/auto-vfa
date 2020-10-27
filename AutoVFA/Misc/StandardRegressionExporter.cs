using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System;
using System.IO;
using System.Linq;

namespace AutoVFA.Misc
{
    internal class StandardRegressionExporter : ExportContextBuilder
    { 
        public override void ExportToXLSX(string fileName)
        {
            try
            {
                var fi = new FileInfo(fileName); 
                using var package = new ExcelPackage(fi);

                var offsetX = 1;
                var offsetY = 1; 
                var wsName = "standard";
                var worksheet = package.Workbook.Worksheets.Any(x => x.Name == wsName) ?
                    package.Workbook.Worksheets[wsName] :
                    package.Workbook.Worksheets.Add(wsName);
                worksheet.Cells.Clear();
                worksheet.Cells[offsetX + 1, offsetY + 1].Value = "Acid";
                worksheet.Cells[offsetX + 1, offsetY + 2].Value = "a";
                worksheet.Cells[offsetX + 1, offsetY + 3].Value = "b";
                worksheet.Cells[offsetX + 1, offsetY + 4].Value = "Rsqr";
                for (var i = 0; i < _results.Count; i++)
                {
                    var result = _results[i];
                    worksheet.Cells[offsetX + i + 2, offsetY + 1].Value = result.Acid;
                    worksheet.Cells[offsetX + i + 2, offsetY + 2].Value = result.A;
                    worksheet.Cells[offsetX + i + 2, offsetY + 3].Value = result.B;
                    worksheet.Cells[offsetX + i + 2, offsetY + 4].Value = result.R2;
                }

                var acids = _availableAcids;

                offsetX += acids.Length + 5;

                for (int i = 0; i < _standards.Count; i++)
                {
                    worksheet.Cells[offsetX - 2, offsetY + 2 + i].Value = _standards[i].Name;
                    worksheet.Cells[offsetX - 1, offsetY + 2 + i].Value = $"Level {i + 1}";
                }

                var rowPad = 2;
                worksheet.Cells[offsetX + rowPad + acids.Length - 1, offsetY + 1].Value = $"Norm-d with int. standard ({_baseNormAcid})";
                using (var range = worksheet.Cells[offsetX + rowPad + acids.Length - 1, offsetY + 1,
                    offsetX + rowPad + acids.Length - 1, offsetY + _standards.Count + 1])
                {
                    range.Merge = true;
                    range.Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                }

                var chart = 0;
                var chartHeight = 14;
                var chartWidth = 6;
                var chartOffsetX = offsetX + acids.Length + 10;
                foreach (var acid in acids.Except(new[] { _baseNormAcid }))
                {
                    var elems = _standards.Select(x => x.AnalysisInfo.First(info => info.Name == acid)).ToArray();
                    var vX = elems.Select(x => x.Result).ToArray();
                    var vY = elems.Select(x => x.Norm).ToArray();

                    worksheet.Cells[offsetX, offsetY + 1].Value = acid;
                    worksheet.Cells[offsetX + rowPad + acids.Length, offsetY + 1].Value = acid;

                    for (int i = 0; i < vX.Length; i++)
                    {
                        worksheet.Cells[offsetX, offsetY + 2 + i].Value = vX[i];
                        worksheet.Cells[offsetX + rowPad + acids.Length, offsetY + 2 + i].Value = vY[i];
                    }

                    var scatterChart = worksheet.Drawings
                        .AddChartFromTemplate(new FileInfo("Templates/template.crtx"), acid)
                        .As.Chart.ScatterChart;
                    scatterChart.Title.Text = acid;
                    scatterChart.SetPosition(chartOffsetX, 0, chart++ * chartWidth, 0);
                    scatterChart.To.Row = scatterChart.From.Row + chartHeight;
                    scatterChart.To.Column = scatterChart.From.Column + chartWidth - 1;
                    var serie = scatterChart.Series.Add(worksheet.Cells[offsetX + rowPad + acids.Length, offsetY + 2, offsetX + rowPad + acids.Length, offsetY + 2 + vX.Length - 1],
                        worksheet.Cells[offsetX, offsetY + 2, offsetX, offsetY + 2 + vX.Length - 1]);
                    serie.TrendLines.Add(eTrendLine.Linear);

                    offsetX += 1;
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
    }
}