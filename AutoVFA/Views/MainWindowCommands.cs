using AutoVFA.Misc;
using AutoVFA.Models;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            RunStandardsRegression();

            var list = StandardsRegressionResults;
            Clipboard.SetText(list.ExportToCSV(), TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyAllCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        private void DataGridMenuCopyCSVCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var sel = (e.OriginalSource as DataGridCell)?.DataContext as RegressionResult;
            if (e.OriginalSource is DataGrid grid)
            {
                var info = grid.SelectedCells.FirstOrDefault();
                if (info != default)
                    sel = info.Item as RegressionResult;
            }
            if (sel == default) return;
            var result = sel;
            Clipboard.SetText(result.GetCsv(), TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyCSVCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        private void DataGridMenuCopyEquationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var sel = (e.OriginalSource as DataGridCell)?.DataContext as RegressionResult;
            if (e.OriginalSource is DataGrid grid)
            {
                var info = grid.SelectedCells.FirstOrDefault();
                if (info != default)
                    sel = info.Item as RegressionResult;
            }
            if (sel == default) return;
            var text = sel.GetEquation();
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
            RunStandardsRegression();

            new StandardRegressionExporter()
                .SetAvailableAcids(GetAvailableAcids(StandardList))
                .SetNormAcid(BaseNormAcid)
                .SetRegressionResults(StandardsRegressionResults)
                .ErrorResolver(ex =>
                {
                    ShowError("Error occurred when tried to export results. " +
                              "\nHere is some info for developer." +
                              $"\n{ex.Message}" +
                              $"\nStacktrace: {ex.StackTrace?.ToString() ?? "stacktrace is empty"}");
                })
                .ExportToXLSX(dialog.FileName);
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


        private void RunRegressionCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RunStandardsRegression();
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
                FileName = "STD*",
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = true,
                AddExtension = true
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            _standardsPaths = dialog.FileNames;
            SaveStandards(_samplesPaths);
            OnHasStandards();
        }

        private void SaveSamples(IEnumerable<string> items)
        {
            var array = items as string[] ?? items.ToArray();
            if (!array.Any()) return;
            var coll = new StringCollection();
            coll.AddRange(array);
            AppSettings.Default.SamplePaths = coll;
            AppSettings.Default.Save();
        }

        private void SaveStandards(IEnumerable<string> items)
        {
            var array = items as string[] ?? items.ToArray();
            if (!array.Any()) return;
            var coll = new StringCollection();
            coll.AddRange(array);
            AppSettings.Default.StandardPaths = coll;
            AppSettings.Default.Save();
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
                AddExtension = true,
                FileName = "*RR*"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            _samplesPaths = dialog.FileNames;
            SaveSamples(_samplesPaths);
            OnHasSamples();
        }

        private void OpenSamplesCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
        }

        #endregion

        #region Samples Analysis

        public static readonly RoutedCommand RunSampAnalysisCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
        };

        private void RunSamplesAnalysisCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards && HasSamples;
        }

        private void RunSamplesAnalysisCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RunSamplesAnalysis();
        }

        #endregion
    }
}