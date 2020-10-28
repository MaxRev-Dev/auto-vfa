using System;
using AutoVFA.Misc;
using AutoVFA.Models;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
            RunStandardRegression();

            var list = StandardRegressionResults;
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
                FileName = "AutoVFA"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            // ensure standards normalized and regression was calculated 
            RunStandardRegression();

            new StandardRegressionExporter()
                .SetStandards(StandardList)
                .SetAvailableAcids(GetAvailableAcids(StandardList))
                .SetNormAcid(BaseNormAcid)
                .SetRegressionResults(StandardRegressionResults)
                .ErrorResolver(DefaultExportResolver)
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
            RunStandardRegression();
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
            if (!TryGetFileNames(out var fileNames)) return;
            _standardsPaths = fileNames;
            SaveStandards(_standardsPaths);
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
            if (!TryGetFileNames(out var fileNames)) return;
            _samplesPaths = fileNames;
            SaveSamples(_samplesPaths);
            OnHasSamples();
        }

        private void OpenSamplesCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards;
            ValidateTableCache(e.CanExecute);
            ValidateStandardTableCache(e.CanExecute);
            ValidateStandardTables(e.CanExecute);
        }

        #endregion

        #region Sample Analysis

        public static readonly RoutedCommand RunSampAnalysisCmd = new RoutedCommand
        {
            InputGestures =
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
        };

        private void RunSampleAnalysisCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasStandards && HasSamples;
            ValidateTableCache(e.CanExecute);
            ValidateSampleTables(e.CanExecute);
        }

        private void RunSampleAnalysisCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RunSampleAnalysis();
        }

        #endregion

        #region List file Additions

        private void OnFileOpenInEditor(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var placement = ((ContextMenu)item.Parent).PlacementTarget as ListBoxItem;

            var data = placement!.DataContext as VFADataItem;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo($"{data!.FileName}")
                {
                    UseShellExecute = true
                }
            };
            StartWaitingAsync(process, data, OnFileDataChanged);
        }

        private void OnFileDataChanged()
        {
            AnalyzeAll();
        }

        private async void StartWaitingAsync(Process process, VFADataItem item, Action completed)
        {
            await Task.Run(() =>
            {
                try
                {
                    process.Start();
                    process.WaitForExit();
                    item.LoadData();
                    Dispatcher.Invoke(completed);
                }
                catch
                {
                    // ignored
                }
            });
        }

        private void OnAddFilesAfterItem(object sender, RoutedEventArgs e)
        {
            AddFilesCore(sender, false);
        }

        private void OnAddFilesBeforeItem(object sender, RoutedEventArgs e)
        {
            AddFilesCore(sender, true);
        }

        private void OnReplaceFileItem(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var placement = ((ContextMenu)item.Parent).PlacementTarget;
            var lb = placement.FindParent<ListBox>();
            if (lb == standardsList)
            {
                if (!TryGetFileNames(out var fileNames))
                {
                    return;
                }
                ReplaceFile(StandardList, fileNames, lb.SelectedIndex);
            }
            else if (lb == samplesList)
            {
                if (!TryGetFileNames(out var fileNames))
                {
                    return;
                }
                ReplaceFile(SamplesList, fileNames, lb.SelectedIndex);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void AddFilesCore(object sender, bool before)
        {
            var item = (MenuItem)sender;
            var placement = ((ContextMenu)item.Parent).PlacementTarget;
            var lb = placement.FindParent<ListBox>();
            if (!TryGetFileNames(out var fileNames))
            {
                return;
            }

            try
            {
                if (lb == standardsList)
                {
                    AddFiles(StandardList, fileNames, lb.SelectedIndex, before);
                }
                else if (lb == samplesList)
                {
                    AddFiles(SamplesList, fileNames, lb.SelectedIndex, before);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (Exception ex)
            {
                DefaultResolver("Can not import files", ex);
            }
        }

        private bool TryGetFileNames(out string[] filenames)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Multiselect = true,
                AddExtension = true
            };
            if ((bool)dialog.ShowDialog(this))
            {
                filenames = dialog.FileNames
                    .OrderBy(x => x.Replace(" ", ""))
                    .ToArray();
                return true;
            }
            filenames = Array.Empty<string>();
            return false;
        }


        #endregion

        private void ExportToXlsxBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HasSamples || !HasStandards)
            {
                ShowError("This table shows cached values. " +
                          "There is no source data to calculate, so you can not export");
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Spreadsheet (*.xlsx)|*.xlsx",
                AddExtension = true,
                FileName = "AutoVFA"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            // ensure standards normalized and regression was calculated 
            RunStandardRegression();
            RunSampleAnalysis();
            new SampleAnalysisExporter()
                .SetCVThreshold(CVCellBrushParameter)
                .SetSummary(GenerateSummary(SamplesList))
                .SetAvailableAcids(GetAvailableAcids(StandardList))
                .SetNormAcid(BaseNormAcid)
                .SetRegressionResults(StandardRegressionResults)
                .ErrorResolver(DefaultExportResolver)
                .ExportToXLSX(dialog.FileName);
        }

        private void DefaultResolver(string mess, Exception ex)
        {
            ShowError(mess +
                $"\n" +
                $"Here is some info for developer." +
                $"\n{ex.Message}" +
                $"\nStacktrace: {ex.StackTrace ?? "stacktrace is empty"}");
        }

        private void DefaultExportResolver(Exception ex)
        { 
            DefaultResolver("Can not open the file. \n" +
                            "It's in use by another program. \n" +
                            "Make sure you have closed the Excel worksheet. \n", ex);
        }

        private void OnSortFiles(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var placement = ((ContextMenu)item.Parent).PlacementTarget;
            var lb = placement.FindParent<ListBox>();
            var index = lb.SelectedIndex;
            Func<VFADataItem, string> orderer = x => x.Name.Replace(" ", "");
            if (lb == standardsList)
            {
                var tmp = StandardList.OrderBy(orderer).ToArray();
                StandardList.Clear();
                StandardList.AddRange(tmp);
            }
            else if (lb == samplesList)
            {
                var tmp = SamplesList.OrderBy(orderer).ToArray();
                SamplesList.Clear();
                SamplesList.AddRange(tmp);
            }
            else
            {
                throw new InvalidOperationException();
            }
            lb.SelectedIndex = index;
        }
    }
}