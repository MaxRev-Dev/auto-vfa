using AutoVFA.Misc;
using AutoVFA.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Meziantou.WpfFontAwesome;
using LineSeries = System.Windows.Controls.DataVisualization.Charting.Compatible.LineSeries;

namespace AutoVFA.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {  
            InitializeComponent();
            LoadSettings();
            DataContext = this;
        }

        private void LoadSettings()
        {
            if (AppSettings.Default.StandardPaths != default)
            {
                _standardsPaths = AppSettings.Default.StandardPaths.Cast<string>().ToArray();
                OnHasStandards();
            }

            if (AppSettings.Default.SamplePaths != default)
            {
                _samplesPaths = AppSettings.Default.SamplePaths.Cast<string>().ToArray();
                OnHasSamples();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveStandards(StandardList.Select(x => x.FileName));
            SaveSamples(SamplesList.Select(x => x.FileName));
        }

        private string[] _standardsPaths;

        private string[] _samplesPaths;

        public string BaseNormAcid { get; set; } = "2 ethyl butyric acid";

        public bool HasSamples => _samplesPaths != default && _samplesPaths.Any();
        public bool HasStandards => _standardsPaths != default && _standardsPaths.Any();

        public static ObservableCollection<VFADataItem> StandardList { get; } = new ObservableCollection<VFADataItem>();
        public static ObservableCollection<VFADataItem> SamplesList { get; } = new ObservableCollection<VFADataItem>();
        public static ObservableCollection<RegressionResult> RegressionResults { get; } = new ObservableCollection<RegressionResult>();
        public static ObservableCollection<Chart> ChartStackItemsSource { get; } = new ObservableCollection<Chart>();
         

        #region Standards

        private void OnHasStandards()
        {
            if (HasStandards)
            {
                StandardList.Clear();
                StandardList.AddRange(_standardsPaths
                    .OrderBy(x => x.Replace(" ", ""))
                    .Select(x => new VFADataItem(x)));
                standardsList.SelectedIndex = 0;
                RunRegression();
            }
            else
            {
                ShowError("You should load standards first!");
            }
        }

        private bool ValidateList(ICollection<VFADataItem> list)
        {
            var check = list.Count < 2;
            if (check)
            {
                ShowError("Regression requires minimum 2 sources");
            }
            return !check;
        }


        private void StandardsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!((sender as ListBox)?.SelectedItem is VFADataItem item))
                return;
            if (item.AnalysisInfo == default)
                item.LoadData();
            SetGridData();
        }

        #endregion

        #region Samples

        private void OnHasSamples()
        {
            if (HasSamples)
            {
                SamplesList.Clear();
                SamplesList.AddRange(
                    _samplesPaths.Select(x => new VFADataItem(x)));
                samplesList.SelectedIndex = 0;
            }
            else
            {
                ShowError("You should load samples first!");
            }
        }

        private void SamplesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!((sender as ListBox)?.SelectedItem is VFADataItem item))
                return;
            if (item.AnalysisInfo == default)
                item.LoadData();
            SetGridData();
        }

        #endregion

        private void ShowError(string errMsg)
        {
            MessageBox.Show(errMsg, "Something happend...",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SetGridData()
        {
            /*if (standardsList.SelectedItem == default) return;
            dataGrid.ItemsSource = ((VFADataItem)standardsList.SelectedItem).AnalysisInfo;*/
        }

        private void OnListBoxDrag(object sender, MouseEventArgs mouseEventArgs)
        {
            if (sender is ListBoxItem draggedItem &&
                mouseEventArgs.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void OnListBoxDrop(object sender, DragEventArgs e)
        {
            var droppedData = e.Data.GetData(typeof(VFADataItem)) as VFADataItem;
            var target = ((ListBoxItem)(sender)).DataContext as VFADataItem;
            var listBox = ((ListBoxItem)sender).FindParent<ListBox>();

            HandleDropInternal(((ListCollectionView)listBox.ItemsSource).SourceCollection as ObservableCollection<VFADataItem>, droppedData, target);

            listBox.Items.Refresh();
        }

        private void HandleDropInternal<T>(ObservableCollection<T> list, T droppedData, T target)
        {
            var removedIdx = list.IndexOf(droppedData);
            var targetIdx = list.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                list.Insert(targetIdx + 1, droppedData);
                list.RemoveAt(removedIdx);
            }
            else
            {
                var remIdx = removedIdx + 1;
                if (list.Count + 1 > remIdx)
                {
                    list.Insert(targetIdx, droppedData);
                    list.RemoveAt(remIdx);
                }
            }
        }


        private void RunRegression()
        {
            var list = StandardList;
            if (!ValidateList(list))
            {
                return;
            }
            // calculate norm 
            foreach (var vfaItem in list)
            {
                vfaItem.EnsureDataLoaded();
                var raw = vfaItem.AnalysisInfo;
                var basic = raw.First(x => x.Name == BaseNormAcid);
                foreach (AnalysisInfo t in raw)
                    t.Norm = t.Counts * 1d / basic.Counts;
            }
            SetGridData();
            RunRegressionAnalysis(list);
            CreateCharts();
        }

        private void CreateCharts()
        {
            ChartStackItemsSource.Clear();
            foreach (var rs in RegressionResults)
            {
                var (vX, vY) = rs.GetSources();
                var dt = vX.Zip(vY, (x, y) => new KeyValuePair<double, double>(x, y)).ToArray();
                var chart = new Chart
                {
                    Title = $"R²={rs.R2:F4}, {rs.GetEquation()}",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    MinHeight = 400,
                    MaxHeight = 1100,
                };

                var serieTrend = new LineSeries
                {
                    Title = "Trendline",
                    ItemsSource = new[]
                    {
                        new KeyValuePair<double, double>(vX.First(), rs.A + rs.B * vX.First()),
                        new KeyValuePair<double, double>(vX.Last(), rs.A + rs.B * vX.Last()),
                    },
                    DependentValuePath = "Value",
                    IndependentValuePath = "Key",
                    IsSelectionEnabled = true,
                };
                chart.Series.Add(serieTrend);

                var serie = new LineSeries
                {
                    Title = rs.Acid,
                    DependentValuePath = "Value",
                    IndependentValuePath = "Key",
                    ItemsSource = dt,
                    IsSelectionEnabled = true,
                };

                chart.Series.Add(serie);
                ChartStackItemsSource.Add(chart);
            }

        }


        private void RunRegressionAnalysis(ICollection<VFADataItem> list)
        {
            // calculate goodness of fit for each acid
            foreach (var vfaDataItem in list) vfaDataItem.EnsureDataLoaded();
            RegressionResults.Clear();

            var result = CalculateRegression(list);
            RegressionResults.AddRange(result);
        }

        private IEnumerable<RegressionResult> CalculateRegression(ICollection<VFADataItem> list)
        {
            var acids = GetAvailableAcids(list);
            foreach (var acid in acids.Except(new[] { BaseNormAcid }))
            {
                var elems = list.Select(x => x.AnalysisInfo.First(info => info.Name == acid)).ToArray();
                var vX = elems.Select(x => x.Result).ToArray();
                var vY = elems.Select(x => x.Norm).ToArray();
                var (a, b) = MathNet.Numerics.Fit.Line(vX, vY);
                var r_2 = MathNet.Numerics.GoodnessOfFit.RSquared(vX.Select(v => a + b * v), vY);
                yield return new RegressionResult(acid, vX, vY, a, b, r_2);
            }
        }

        private IEnumerable<string> GetAvailableAcids(ICollection<VFADataItem> list)
        {
            var names = list.Select(x =>
                x.AnalysisInfo.Select(v => v.Name)
                    .Where(name => !string.IsNullOrEmpty(name))).ToArray();
            return names.Aggregate((acc, x) => acc.Zip(x, (s, s1) => s));
        }

        private void OnListBoxDropGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);
            Mouse.SetCursor(e.Effects.HasFlag(DragDropEffects.Move) ? Cursors.Cross : Cursors.No);
            e.Handled = true;
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(decimal) ||
                e.PropertyType == typeof(double) ||
                e.PropertyType == typeof(float))
                ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:F4}";
        }

        private void previewVFADatatable_AutoGeneratedColumns(object sender, EventArgs e)
        {
            if (sender is DataGrid grid)
            {
                foreach (var prop in typeof(AnalysisInfo).GetProperties().Where(x => !x.GetCustomAttributes<PrimAttribute>().Any()))
                {
                    var col = grid.Columns.FirstOrDefault(x => x.Header as string == prop.Name);
                    if (col != default)
                        col.Visibility = Visibility.Hidden;
                }
            }
        }

        private void PreviewVFADatatable_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                var el = ((UIElement)e.OriginalSource);
                if (el.FindParent<DataGridColumnHeader>() != default)

                {
                    var col = grid.Columns;
                    var cm = new ContextMenu();
                    foreach (var dataGridColumn in col)
                    {
                        var it = new MenuItem
                        {
                            Header = dataGridColumn.Header,
                            Icon = new FontAwesomeIcon
                            {
                                SolidIcon = dataGridColumn.Visibility == Visibility.Hidden ?
                                    FontAwesomeSolidIcon.EyeSlash :
                                    FontAwesomeSolidIcon.Eye
                            }
                        };
                        it.Click += (s, ev) =>
                        {
                            var column = grid.Columns.First(x =>
                                  x.Header == ((MenuItem)ev.OriginalSource).Header);
                            column.Visibility = column.Visibility == Visibility.Hidden ?
                                Visibility.Visible : Visibility.Hidden;
                        };
                        cm.Items.Add(it);
                    }
                    grid.ContextMenu = cm;
                    grid.ContextMenu.IsOpen = true;
                }
                else
                {
                    grid.ContextMenu = default;
                }
            }
        }

        private void RegresionGridContextMenu_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                var el = ((UIElement)e.OriginalSource);
                if (el.FindParent<DataGridColumnHeader>() != default)
                {
                    e.Handled = true;
                }
            }
        }
    }
}
