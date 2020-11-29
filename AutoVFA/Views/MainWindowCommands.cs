using AutoVFA.Misc;
using AutoVFA.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoVFA.Views
{
    public partial class MainWindow
    {
        private void DefaultResolver(string mess, Exception ex)
        {
            ShowError(mess +
                      "\n" +
                      "Here is some info for developer." +
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
            UIElement placement = ((ContextMenu)item.Parent).PlacementTarget;
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

        #region Export

        private async void ExportToCsvBtn_OnClick(object sender,
            RoutedEventArgs e)
        {
            if (!_context.HasSamples || !_context.HasStandards)
                ShowError("This table shows cached values. " +
                          "There is no source data to calculate, so you can not export");

            var dialog = new SaveFileDialog
            {
                Filter = "Comma Separated Value (*.csv)|*.csv",
                AddExtension = true,
                FileName = "AutoVFA"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            await Task.Run(async () =>
            {
                // ensure standards normalized and regression was calculated 
                await RunStandardRegression();

                await new SampleAnalysisExporter(_context)
                    .SetModelGroups(GetModelGroups(SamplesList))
                    .SetCVThreshold(ValueCellBrushParameter)
                    .SetSummary(GenerateSummary(SamplesList))
                    .SetAvailableAcids(GetAvailableAcids(StandardList))
                    .SetRegressionResults(StandardRegressionResults)
                    .ErrorResolver(DefaultExportResolver)
                    .ExportToCsv(dialog.FileName);
                Dispatcher.Invoke(RestoreScroll);
            });
        }

        private async void ExportToXlsxBtn_OnClick(object sender,
            RoutedEventArgs e)
        {
            if (!_context.HasSamples || !_context.HasStandards)
                ShowError("This table shows cached values. " +
                          "There is no source data to calculate, so you can not export");

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Spreadsheet (*.xlsx)|*.xlsx",
                AddExtension = true,
                FileName = "AutoVFA"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            await Task.Run(async () =>
            {
                // ensure standards normalized and regression was calculated 
                await RunStandardRegression();

                await new SampleAnalysisExporter(_context)
                    .SetCVThreshold(ValueCellBrushParameter)
                    .SetSummary(GenerateSummary(SamplesList))
                    .SetAvailableAcids(GetAvailableAcids(StandardList))
                    .SetRegressionResults(StandardRegressionResults)
                    .ErrorResolver(DefaultExportResolver)
                    .ExportToXLSX(dialog.FileName);
                Dispatcher.Invoke(RestoreScroll);
            });
        }

        #endregion

        #region DataGrid

        public static readonly RoutedCommand DataGridMenuCopyEquationCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.F, ModifierKeys.Control)
                }
            };

        public static readonly RoutedCommand DataGridMenuCopyCSVCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.C, ModifierKeys.Control)
                }
            };

        public static readonly RoutedCommand DataGridMenuCopyAllCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.A, ModifierKeys.Control)
                }
            };

        public static readonly RoutedCommand DataGridMenuExportCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.E, ModifierKeys.Control)
                }
            };

        private async void DataGridMenuCopyAllCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            // ensure standards normalized and regression was calculated 
            await RunStandardRegression();

            var list = StandardRegressionResults;
            Clipboard.SetText(list.ExportToCSV(),
                TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyAllCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
        }

        private void DataGridMenuCopyCSVCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            var sel =
                (e.OriginalSource as DataGridCell)?.DataContext as
                RegressionResult;
            if (e.OriginalSource is DataGrid grid)
            {
                DataGridCellInfo info = grid.SelectedCells.FirstOrDefault();
                if (info != default)
                    sel = info.Item as RegressionResult;
            }

            if (sel == default) return;
            RegressionResult result = sel;
            Clipboard.SetText(result.GetCsv(),
                TextDataFormat.CommaSeparatedValue);
        }

        private void DataGridMenuCopyCSVCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
        }

        private void DataGridMenuCopyEquationCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            var sel =
                (e.OriginalSource as DataGridCell)?.DataContext as
                RegressionResult;
            if (e.OriginalSource is DataGrid grid)
            {
                DataGridCellInfo info = grid.SelectedCells.FirstOrDefault();
                if (info != default)
                    sel = info.Item as RegressionResult;
            }

            if (sel == default) return;
            var text = sel.GetEquation();
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
        }

        private void DataGridMenuCopyEquationCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
        }

        private async void DataGridMenuExportCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Spreadsheet (*.xlsx)|*.xlsx",
                AddExtension = true,
                FileName = "AutoVFA"
            };
            if (!(bool)dialog.ShowDialog(this)) return;
            // ensure standards normalized and regression was calculated 
            await RunStandardRegression();

            await new StandardRegressionExporter(_context)
                .SetStandards(StandardList)
                .SetAvailableAcids(GetAvailableAcids(StandardList))
                .SetRegressionResults(StandardRegressionResults)
                .ErrorResolver(DefaultExportResolver)
                .ExportToXLSX(dialog.FileName);
        }

        private void DataGridMenuExportCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
        }

        #endregion

        #region Regression

        public static readonly RoutedCommand RunStdRegressionCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.R, ModifierKeys.Control)
                }
            };


        private async void RunRegressionCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            await RunStandardRegression();
        }

        private void RunRegressionCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
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

        private void OpenStandardsCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            if (!TryGetFileNames(out var fileNames)) return;
            _context.SetStandards(fileNames);
            SaveStandards(_context.StandardsPaths);
            OnHasStandards();
            if (_context.HasSamples)
                OnHasSamples();
        }

        private void SaveSamples(IEnumerable<string> items)
        {
            var array = items as string[] ?? items.ToArray();
            var coll = new StringCollection();
            coll.AddRange(array);
            AppSettings.Default.SamplePaths = coll;
            AppSettings.Default.Save();
        }

        private void SaveStandards(IEnumerable<string> items)
        {
            var array = items as string[] ?? items.ToArray();
            var coll = new StringCollection();
            coll.AddRange(array);
            AppSettings.Default.StandardPaths = coll;
            AppSettings.Default.Save();
        }

        private void OpenStandardsCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
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

        private void OpenSamplesCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            if (!TryGetFileNames(out var fileNames)) return;
            _context.SetSamples(fileNames);
            SaveSamples(_context.SamplesPaths);
            OnHasSamples();
        }

        private void OpenSamplesCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards;
            ValidateTableCache(e.CanExecute);
            ValidateStandardTableCache(e.CanExecute);
            ValidateStandardTables(e.CanExecute);
        }

        #endregion

        #region Sample Analysis

        public static readonly RoutedCommand RunSampAnalysisCmd =
            new RoutedCommand
            {
                InputGestures =
                {
                    new KeyGesture(Key.S,
                        ModifierKeys.Control | ModifierKeys.Shift)
                }
            };

        private void RunSampleAnalysisCmdCanExecute(object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _context.HasStandards && _context.HasSamples;
            ValidateTableCache(e.CanExecute);
            ValidateSampleTables(e.CanExecute);
        }

        private async void RunSampleAnalysisCmdExecuted(object sender,
            ExecutedRoutedEventArgs e)
        {
            await AnalyzeAll();
        }

        #endregion

        #region List file Additions

        private void OnFileOpenInEditor(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var placement =
                ((ContextMenu)item.Parent).PlacementTarget as ListBoxItem;

            var data = placement!.DataContext as VFADataItem;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo($"{data!.FileName}")
                {
                    UseShellExecute = true
                }
            };
            StartWaitingAsync(process, data);
        }


        private async void StartWaitingAsync(Process process, VFADataItem item)
        {
            await Task.Run(async () =>
            {
                try
                {
                    process.Start();
                    process.WaitForExit();
                    await item.LoadData();
                    await AnalyzeAll();
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
            UIElement placement = ((ContextMenu)item.Parent).PlacementTarget;
            var lb = placement.FindParent<ListBox>();
            if (lb == standardsList)
            {
                if (!TryGetFileNames(out var fileNames)) return;
                ReplaceFile(StandardList, fileNames, lb.SelectedIndex);
                _context.SetStandards(StandardList.Select(x => x.FileName)
                    .ToArray());
            }
            else if (lb == samplesList)
            {
                if (!TryGetFileNames(out var fileNames)) return;
                ReplaceFile(SamplesList, fileNames, lb.SelectedIndex);
                _context.SetSamples(SamplesList.Select(x => x.FileName)
                    .ToArray());
            }
            else
            {
                throw new InvalidOperationException();
            }

            RefreshItemsAsync(lb);
        }

        private void AddFilesCore(object sender, bool before)
        {
            var item = (MenuItem)sender;
            UIElement placement = ((ContextMenu)item.Parent).PlacementTarget;
            var lb = placement.FindParent<ListBox>();
            if (!TryGetFileNames(out var fileNames)) return;

            try
            {
                if (lb == standardsList)
                {
                    AddFiles(StandardList, fileNames, lb.SelectedIndex, before);
                    _context.SetStandards(StandardList.Select(x => x.FileName)
                        .ToArray());
                }
                else if (lb == samplesList)
                {
                    AddFiles(SamplesList, fileNames, lb.SelectedIndex, before);
                    _context.SetSamples(SamplesList.Select(x => x.FileName)
                        .ToArray());
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

            RefreshItemsAsync(lb);
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

        private void DangerThresholdChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;
            if (!float.TryParse(tb.Text, out var val)) return;
            ValueCellBrushParameter.Danger = val; 
            RecalcCounter[nameof(DangerColorThreshold)] = true;
        }

        private void WarningThresholdChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;
            if (!float.TryParse(tb.Text, out var val)) return;
            ValueCellBrushParameter.Warning = val; 
            RecalcCounter[nameof(WarnThreshold)] = true;
        }

        private void DangerColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox cb)) return;
            if (!(cb.SelectedItem is UIElement el))
                return;
            if (!(el.GetChildOfType<Grid>() is { } grid)) return;
            ValueCellBrushParameter.DangerColor = ((SolidColorBrush)grid.Background).Color;
            RecalcCounter[nameof(DangerColorBox)] = true;
        }

        private void WarningColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox cb)) return;
            if (!(cb.SelectedItem is UIElement el))
                return;
            if (!(el.GetChildOfType<Grid>() is { } grid)) return;
            ValueCellBrushParameter.WarningColor = ((SolidColorBrush)grid.Background).Color;
            RecalcCounter[nameof(WarnColorBox)] = true;
        }
    }
}