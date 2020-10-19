using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoVFA.Misc;
using AutoVFA.Models;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace AutoVFA.Views
{
    public partial class MainWindow
    {
        #region DataGrid

        public static readonly RoutedCommand DataGridMenuCopyEquationCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.F, ModifierKeys.Control )
            }
        };

        public static readonly RoutedCommand DataGridMenuCopyCSVCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.C, ModifierKeys.Control)
            }
        };

        public static readonly RoutedCommand DataGridMenuCopyAllCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.A, ModifierKeys.Control)
            }
        };

        public static readonly RoutedCommand DataGridMenuExportCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.E, ModifierKeys.Control)
            }
        };

        private void DataGridMenuCopyAllCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // ensure standards normalized and regression was calculated 
            RunRegression();

            var list = RegressionResults;
            Clipboard.SetText(list.ExportToCSV(), TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyAllCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        private void DataGridMenuCopyCSVCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {  
            var result = (RegressionResult)((DataGridCell)e.OriginalSource).DataContext;
            Clipboard.SetText(result.GetCsv(), TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyCSVCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        private void DataGridMenuCopyEquationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        { 
            var result = (RegressionResult)((DataGridCell)e.OriginalSource).DataContext;
            var text = result.GetEquation();
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
        }

        private void DataGridMenuCopyEquationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;

        }

        private void DataGridMenuExportCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Spreadsheet (*.xlsx)|*.xlsx",
                AddExtension = true,
                FileName = "StandardsRegression"
            };
            if (!(bool)dialog.ShowDialog(this)) return;

            // ensure standards normalized and regression was calculated 
            RunRegression();

            var list = StandardList;
            var fi = new FileInfo(dialog.FileName);
            fi.Delete();
            using var package = new ExcelPackage(fi);

            var offsetX = 1;
            var offsetY = 1;
            var worksheet = package.Workbook.Worksheets.Add("standard");
            worksheet.Cells[offsetX + 1, offsetY + 1].Value = "Acid";
            worksheet.Cells[offsetX + 1, offsetY + 2].Value = "a";
            worksheet.Cells[offsetX + 1, offsetY + 3].Value = "b";
            worksheet.Cells[offsetX + 1, offsetY + 4].Value = "Rsqr";
            for (var i = 0; i < RegressionResults.Count; i++)
            {
                var result = RegressionResults[i];
                worksheet.Cells[offsetX + i + 2, offsetY + 1].Value = result.Acid;
                worksheet.Cells[offsetX + i + 2, offsetY + 2].Value = result.A;
                worksheet.Cells[offsetX + i + 2, offsetY + 3].Value = result.B;
                worksheet.Cells[offsetX + i + 2, offsetY + 4].Value = result.R2;
            }
            worksheet.Cells.AutoFitColumns(0);

            var acids = GetAvailableAcids(list).ToArray();

            offsetX += acids.Length + 5;

            for (int i = 0; i < list.Count; i++)
            {
                worksheet.Cells[offsetX - 2, offsetY + 2 + i].Value = list[i].Name;
                worksheet.Cells[offsetX - 1, offsetY + 2 + i].Value = $"Level {i + 1}";
            }

            var rowPad = 2;
            worksheet.Cells[offsetX + rowPad + acids.Length - 1, offsetY + 1].Value = $"Norm-d with int. standard ({BaseNormAcid})";
            using (var range = worksheet.Cells[offsetX + rowPad + acids.Length - 1, offsetY + 1,
                offsetX + rowPad + acids.Length - 1, offsetY + list.Count + 1])
            {
                range.Merge = true;
                range.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
            }

            var chart = 0;
            var chartHeight = 14;
            var chartWidth = 6;
            var chartOffsetX = offsetX + acids.Length + 10;
            foreach (var acid in acids.Except(new[] { BaseNormAcid }))
            {
                var elems = list.Select(x => x.AnalysisInfo.First(info => info.Name == acid)).ToArray();
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

        private void DataGridMenuExportCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }
        #endregion

        #region Regression

        public static readonly RoutedCommand RunStdRegressionCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.R, ModifierKeys.Control)
            }
        };

        public static readonly RoutedCommand RunSampRegressionCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
        };


        private void RunRegressionCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RunRegression();
        }

        private void RunRegressionCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        #endregion

        #region Standards

        public static readonly RoutedCommand OpenStandards = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)
            }
        };

        private void OpenStandardsCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = true,
                AddExtension = true
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            _standardsPaths = dialog.FileNames;
            OnHasStandards();
        }

        private void OpenStandardsCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Samples

        public static readonly RoutedCommand OpenSamples = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.O, ModifierKeys.Control)
            }
        };

        private void OpenSamplesCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = true,
                AddExtension = true
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            _samplesPaths = dialog.FileNames;
            OnHasSamples();
        }

        private void OpenSamplesCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        #endregion
    }
}