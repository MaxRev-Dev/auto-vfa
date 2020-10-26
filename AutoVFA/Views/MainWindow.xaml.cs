using AutoVFA.Misc;
using AutoVFA.Models;
using Meziantou.WpfFontAwesome;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using Expression = System.Windows.Expression;
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
        public static ObservableCollection<RegressionResult> StandardsRegressionResults { get; } = new ObservableCollection<RegressionResult>();
        public static ObservableCollection<Chart> StandardsChartItemsSource { get; } = new ObservableCollection<Chart>();
        public static ObservableCollection<UIElement> ModelAnalysisItemsSource { get; } = new ObservableCollection<UIElement>();

        public static CVThreshold CVCellBrushParameter
        {
            get {
                var str = AppSettings.Default.CVThreshold;
                CVThreshold val;
                if (str == default)
                {
                    val = new CVThreshold();
                    AppSettings.Default.CVThreshold = val;
                    AppSettings.Default.Save();
                }
                else
                {
                    val = AppSettings.Default.CVThreshold;
                }
                return val;
            }
        }


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
                RunStandardsRegression();
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
        }

        #endregion

        #region Samples

        private void OnHasSamples()
        {
            if (HasSamples)
            {
                SamplesList.Clear();
                SamplesList.AddRange(
                    _samplesPaths.OrderBy(x => x).Select(x => new VFADataItem(x)));
                samplesList.SelectedIndex = 0;
                RunSamplesAnalysis();
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
        }

        #endregion

        private void ShowError(string errMsg)
        {
            MessageBox.Show(errMsg, "Something happend...",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region Drag&Drop

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

        private static void HandleDropInternal<T>(IList<T> list, T droppedData, T target)
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


        #endregion

        private void RunStandardsRegression()
        {
            var list = StandardList;
            if (!ValidateList(list))
            {
                return;
            }
            CalculateNorm(list);
            RunRegressionAnalysis(list, StandardsRegressionResults);
            CreateCharts(StandardsChartItemsSource, StandardsRegressionResults);
        }

        private void RunSamplesAnalysis()
        {
            var list = SamplesList;
            if (!ValidateList(list))
            {
                return;
            }
            CalculateNorm(list);
            var groups = Extensions.GetSimilarFileNames(list.Select(x => x.FileName));
            var sampleAnalysis = FindConcentration(StandardsRegressionResults, list).ToArray();

            var models = new List<ModelGroup>();
            foreach (var pair in groups)
            {
                var model = new ModelGroup(sampleAnalysis.First(a =>
                    a.Name == Path.GetFileNameWithoutExtension(pair.Key)));
                model.Add(pair.Value.Select(x => sampleAnalysis.First(a =>
                    a.Name == Path.GetFileNameWithoutExtension(x))).ToArray());
                models.Add(model);
            }


            var acids = GetAvailableAcids(list).ToArray();

            var grids = new List<UIElement>();
            var summaryTypes = new Expression<Func<AcidSummary, double>>[]
            {
                x => x.Average_mM,
                x => x.CV_mM,
                x => x.Average_Fraction,
                x => x.CV_Fraction,
            };
            foreach (var summaryType in summaryTypes)
            {
                var azType = ((MemberExpression)summaryType.Body).Member.Name;
                var grid = new DataGrid
                {
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    RowHeaderTemplate = this.FindResource("DataGridRowHeaderTemplate") as DataTemplate,
                };
                grid.AutoGeneratedColumns += (h1, h2) =>
                     SampleAnalysis_AutoGeneratedColumns(h1, h2, azType.Contains("CV"));
                grid.AutoGeneratingColumn += SampleAnalysis_AutoGeneratingColumn;
                var acidsForModel = new List<AcidViewModel>();
                foreach (var acid in acids.Except(new[] { BaseNormAcid }))
                {
                    var dict = new BindableDynamicDictionary();
                    foreach (var model in models)
                    {
                        var summary = new AcidSummary(model, acid);
                        var result = summaryType.Compile()(summary);
                        dict[model.Name] = result;
                    }
                    acidsForModel.Add(new AcidViewModel(acid, dict));
                }

                grid.ItemsSource = acidsForModel;

                var textBlock = new TextBlock
                {
                    Text = azType,
                    FontWeight = FontWeights.Bold,
                    FontSize = 17
                };
                var container = new StackPanel { Margin = new Thickness(5) };
                container.Children.Add(textBlock);
                container.Children.Add(grid);
                grids.Add(container);
            }

            ModelAnalysisItemsSource.Clear();
            ModelAnalysisItemsSource.AddRange(grids);
        }

        private IEnumerable<SampleAnalysis> FindConcentration(ICollection<RegressionResult> standards,
            IEnumerable<VFADataItem> samples)
        {
            foreach (var sample in samples)
            {
                var sampleAnalysis = new SampleAnalysis(sample);
                foreach (var regressionResult in standards)
                {
                    var target = sample.AnalysisInfo.First(x => x.Name == regressionResult.Acid);
                    var acidConcentration = regressionResult.Concentration(target.Norm);
                    sampleAnalysis.SetConcentration(regressionResult.Acid, acidConcentration);
                }
                yield return sampleAnalysis;
            }
        }

        private void CalculateNorm(IEnumerable<VFADataItem> list)
        {
            foreach (var vfaItem in list)
            {
                vfaItem.EnsureDataLoaded();
                var raw = vfaItem.AnalysisInfo;
                var basic = raw.First(x => x.Name == BaseNormAcid);
                foreach (AnalysisInfo t in raw)
                    t.Norm = t.Counts * 1d / basic.Counts;
            }
        }

        private void CreateCharts(ICollection<Chart> target,
            IEnumerable<RegressionResult> regressionResults)
        {
            target.Clear();
            foreach (var rs in regressionResults)
            {
                var (vX, vY) = rs.GetSources();
                var dt = vX.Zip(vY, (x, y) => new KeyValuePair<double, double>(x, y)).ToArray();
                var chart = new Chart
                {
                    Title = new StackPanel()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"{rs.Acid}",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                FontWeight = FontWeights.Bold,
                                FontSize = 18
                            },
                            new TextBlock
                            {
                                Text = $"R²={rs.R2:F4}, {rs.GetEquation()}",
                                HorizontalAlignment = HorizontalAlignment.Center,
                            }
                        }
                    },
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
                target.Add(chart);
            }

        }


        private void RunRegressionAnalysis(ICollection<VFADataItem> list, ICollection<RegressionResult> target)
        {
            foreach (var vfaDataItem in list) vfaDataItem.EnsureDataLoaded();
            target.Clear();

            // calculate goodness of fit for each acid
            var result = CalculateRegression(list);
            foreach (var regressionResult in result)
                target.Add(regressionResult);
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
                foreach (var prop in typeof(AnalysisInfo).GetProperties()
                    .Where(x => !x.GetCustomAttributes<PrimAttribute>().Any()))
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

        private void SampleAnalysis_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyDescriptor is PropertyDescriptor d)
            {
                if (d.ComponentType == typeof(AcidViewModel))
                {
                    e.Cancel = true;
                }
            }
        }

        private void SampleAnalysis_AutoGeneratedColumns(object sender, EventArgs e, bool applyCellStyle)
        {
            if (!(sender is DataGrid dg)) return;
            var values = dg.ItemsSource.Cast<object>().Take(1);
            foreach (var first in values)
            {
                if (first is AcidViewModel avm)
                {
                    var cellStyle =
                        this.FindResource("DataGridCellTemplate" + (applyCellStyle ? "Colored" : "")) as DataTemplate;
                    var names = avm.Values.GetDynamicMemberNames();
                    foreach (var name in names)
                    {
                        dg.Columns.Add(new DataGridTemplateColumn
                        {
                            Header = name,
                            CellTemplate = cellStyle,
                        });
                    }
                }
            }
        }

        private void StandardsList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is ListBox lb)) return;
            HandleListKeyAction(lb, e, ref _standardsPaths, StandardList);
        }

        private void SamplesList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is ListBox lb)) return;
            HandleListKeyAction(lb, e, ref _samplesPaths, SamplesList);
        }

        private void HandleListKeyAction(ListBox lb, KeyEventArgs e, ref string[] source, IList<VFADataItem> target)
        {
            if (e.Key == Key.Delete)
            {
                var except = lb.SelectedItems
                    .Cast<VFADataItem>().ToArray();
                source = source.Except(except.Select(x => x.FileName)).ToArray();
                foreach (var item in except)
                    target.Remove(item);
            }
        }
    }

}
