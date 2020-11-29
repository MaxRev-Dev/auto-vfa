using AutoVFA.Annotations;
using AutoVFA.Misc;
using AutoVFA.Models;
using DrWPF.Windows.Data;
using MathNet.Numerics;
using Meziantou.WpfFontAwesome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Control = System.Windows.Controls.Control;
using LineSeries =
    System.Windows.Controls.DataVisualization.Charting.Compatible.LineSeries;
using Window = System.Windows.Window;

namespace AutoVFA.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int ScrollLoopbackTimeout = 500;
        private readonly AnalyzingContext _context = new AnalyzingContext();
        private int _lastScrollChange = Environment.TickCount;

        private object _lastScrollingElement;
        private double _lastScrollOffset;
        private ObservableDictionary<string, bool> _recalcCounter = new ObservableDictionary<string, bool>();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            DataContext = this;
            ((INotifyPropertyChanged)_recalcCounter).PropertyChanged
                += (s, e) => OnPropertyChanged(nameof(RecalcCounter));
            Closed += (s, e) =>
            {
                AppSettings.Default.ValueConfig = Extensions.ToJsonString(ValueCellBrushParameter);
                AppSettings.Default.Save();
            };
        }

        public static ObservableCollection<VFADataItem> StandardList { get; } =
            new ObservableCollection<VFADataItem>();

        public static ObservableCollection<VFADataItem> SamplesList { get; } =
            new ObservableCollection<VFADataItem>();

        public static ObservableCollection<RegressionResult>
            StandardRegressionResults
        { get; } =
            new ObservableCollection<RegressionResult>();

        public static ObservableCollection<Chart> StandardsChartItemsSource
        {
            get;
        } = new ObservableCollection<Chart>();

        public static ObservableCollection<UIElement> ModelAnalysisItemsSource
        {
            get;
        } = new ObservableCollection<UIElement>();

        public static ValueThresholdConfig _valConfig;
        public static ValueThresholdConfig ValueCellBrushParameter
        {
            get {
                if (_valConfig != default) return _valConfig;
                if (!string.IsNullOrEmpty(AppSettings.Default.ValueConfig))
                {
                    try
                    {
                        return _valConfig = Extensions.FromJson<ValueThresholdConfig>
                        (AppSettings.Default.ValueConfig);
                    }
                    catch
                    {
                        // let's just create new instance
                    }
                }
                _valConfig = new ValueThresholdConfig();
                AppSettings.Default.ValueConfig = Extensions.ToJsonString(_valConfig);
                AppSettings.Default.Save();
                return _valConfig;
            }
        }

        public ObservableDictionary<string, bool> RecalcCounter
        {
            get => _recalcCounter;
            private set {
                _recalcCounter = value;
                OnPropertyChanged(nameof(RecalcCounter));
            }
        }

        private void LoadSettings()
        {
            string[] ignored1 = Array.Empty<string>(),
                ignored2 = Array.Empty<string>();
            if (AppSettings.Default.StandardPaths != default)
            {
                _context.SetStandards(AppSettings.Default.StandardPaths
                    .Cast<string>().ToArray(), out ignored1);
                if (_context.HasStandards)
                    OnHasStandards();
            }

            if (AppSettings.Default.SamplePaths != default)
            {
                _context.SetSamples(AppSettings.Default.SamplePaths
                    .Cast<string>().ToArray(), out ignored2);
                if (_context.HasSamples)
                    OnHasSamples();
            }

            var sb = new StringBuilder();
            if (ignored1.Any())
            {
                sb.AppendLine("Some standard files were not loaded:\n " +
                              $"{string.Join("\n", ignored1.Take(5))}");
                if (ignored1.Length > 5)
                    sb.AppendLine($"And {ignored1.Length - 5} more.");
            }

            if (ignored2.Any())
            {
                sb.AppendLine("Some sample files were not loaded:\n " +
                              $"{string.Join("\n", ignored2.Take(5))}");
                if (ignored1.Length > 5)
                    sb.AppendLine($"And {ignored2.Length - 5} more.");
            }

            if (sb.Length > 0) ShowError(sb.ToString());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveStandards(StandardList.Select(x => x.FileName));
            SaveSamples(SamplesList.Select(x => x.FileName));
        }

        private void ShowError(string errMsg)
        {
            MessageBox.Show(errMsg, "Something happend...",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task RunStandardRegression()
        {
            var list = StandardList;
            if (!ValidateListBeforeRegression(list)) return;

            await Task.Run(async () =>
            {
                await CalculateNorm(list);
                await RunRegressionAnalysis(list, StandardRegressionResults);
            });
            Dispatcher.Invoke(() =>
                CreateChartsUI(StandardsChartItemsSource,
                    StandardRegressionResults));
        }

        private async Task RunSampleAnalysis()
        {
            if (!_context.HasSamples) return;


            Dispatcher.Invoke(() =>
            {
                LoaderGrid.Visibility = Visibility.Visible;
                ModelAnalysisItemsSource.Clear();
            });
            var list = SamplesList;
            if (!ValidateListBeforeRegression(list)) return;

            await foreach (var (summary, summaryType) in GenerateSummary(list))
                Dispatcher.Invoke(() =>
                {
                    var container = new StackPanel { Margin = new Thickness(5) };
                    var azType = ((MemberExpression)summaryType.Body).Member
                        .Name;
                    var textBlock = new TextBlock
                    {
                        Text = azType,
                        FontWeight = FontWeights.Bold,
                        FontSize = 17
                    };
                    var grid = new DataGrid
                    {
                        HeadersVisibility = DataGridHeadersVisibility.All,
                        RowHeaderTemplate =
                            FindResource("DataGridRowHeaderTemplate") as
                                DataTemplate,
                        ItemsSource = summary
                    };
                    container.Children.Add(textBlock);
                    container.Children.Add(grid);
                    grid.AutoGeneratedColumns += (h1, h2) =>
                        SampleAnalysis_AutoGeneratedColumns(h1,
                            azType.Contains("CV"));
                    grid.AutoGeneratingColumn +=
                        SampleAnalysis_AutoGeneratingColumn;
                    grid.LayoutUpdated += (_, __) =>
                    {
                        ScrollViewer sc = grid.GetScrollViewer();
                        if (sc != default)
                        {
                            sc.ScrollChanged -=
                                SummaryDataGridScrollChanged;
                            sc.ScrollChanged +=
                                SummaryDataGridScrollChanged;
                        }
                    };
                    ModelAnalysisItemsSource.Add(container);
                });

            Dispatcher.Invoke(() =>
            {
                LoaderGrid.Visibility = Visibility.Collapsed;
                RecalcCounter.Clear();
            });
        }

        private void SummaryDataGridScrollChanged(object sender,
            ScrollChangedEventArgs e)
        {
            if (_lastScrollingElement != sender &&
                Environment.TickCount - _lastScrollChange <
                ScrollLoopbackTimeout) return;
            _lastScrollingElement = sender;
            _lastScrollChange = Environment.TickCount;
            var s = (ScrollViewer)sender;
            _lastScrollOffset = s.HorizontalOffset;

            foreach (ScrollViewer scr in ModelAnalysisItemsSource
                .Select(x => x.GetScrollViewer()).Except(new[] { s }))
                scr.ScrollToHorizontalOffset(_lastScrollOffset);
        }

        private void RestoreScroll()
        {
            if (!(_lastScrollingElement is ScrollViewer s)) return;
            foreach (ScrollViewer scr in ModelAnalysisItemsSource
                .Select(x => x.GetScrollViewer()))
                scr.ScrollToHorizontalOffset(s.HorizontalOffset);
        }

        private async IAsyncEnumerable<(IList<AcidViewModel> model,
                Expression<Func<AcidSummary, double>>)>
            GenerateSummary(ICollection<VFADataItem> list)
        {
            await CalculateNorm(list);
            await EnsureStandardRegression();
            var models = GetModelGroups(list).ToArray();
            var acids = GetAvailableAcids(list).ToArray();
            foreach (var summaryType in GetSummaryTypes())
            {
                var sx = summaryType.Compile();
                var acidsForModel = new List<AcidViewModel>();
                foreach (var acid in acids.Except(new[] { _context.BaseNormAcid }))
                {
                    var dict = new BindableDynamicDictionary();
                    foreach (ModelGroup model in models)
                    {
                        var summary = new AcidSummary(model, acid);
                        var result = sx(summary);
                        dict[model.Name] = result;
                    }

                    acidsForModel.Add(new AcidViewModel(acid, dict));
                }

                yield return (acidsForModel, summaryType);
            }
        }

        private IEnumerable<ModelGroup> GetModelGroups(
            ICollection<VFADataItem> list)
        {
            var groups =
                Extensions.GetSimilarFileNames(list.Select(x => x.FileName));
            var sampleAnalysis =
                FindConcentration(StandardRegressionResults, list).ToArray();
            foreach (var pair in groups)
            {
                SampleAnalysis target = sampleAnalysis.FirstOrDefault(a =>
                    a.Name == Path.GetFileNameWithoutExtension(pair.Key));
                if (target == default)
                    continue;
                var model = new ModelGroup(target);
                model.Add(pair.Value.Select(x => sampleAnalysis.First(a =>
                    a.Name == Path.GetFileNameWithoutExtension(x))).ToArray());
                yield return model;
            }
        }

        private async Task EnsureStandardRegression()
        {
            if (!StandardRegressionResults.Any())
                await RunRegressionAnalysis(StandardList,
                    StandardRegressionResults);
        }

        private IEnumerable<Expression<Func<AcidSummary, double>>>
            GetSummaryTypes()
        {
            return new Expression<Func<AcidSummary, double>>[]
            {
                x => x.Average_mM,
                x => x.CV_mM,
                x => x.Average_Fraction,
                x => x.CV_Fraction
            };
        }

        private IEnumerable<SampleAnalysis> FindConcentration(
            ICollection<RegressionResult> standards,
            IEnumerable<VFADataItem> samples)
        {
            samples = samples.Where(x => x.Loaded).ToArray();
            foreach (VFADataItem sample in samples)
            {
                var sampleAnalysis = new SampleAnalysis(sample);
                foreach (RegressionResult regressionResult in standards)
                {
                    AnalysisInfo target =
                        sample.AnalysisInfo.First(x =>
                            x.Name == regressionResult.Acid);
                    var acidConcentration =
                        regressionResult.Concentration(target.Norm);
                    sampleAnalysis.SetConcentration(regressionResult.Acid,
                        acidConcentration);
                }

                yield return sampleAnalysis;
            }
        }

        private async Task CalculateNorm(IEnumerable<VFADataItem> list)
        {
            foreach (VFADataItem vfaItem in list)
            {
                if (!await vfaItem.EnsureDataLoadedAsync()) continue;
                var raw = vfaItem.AnalysisInfo;
                AnalysisInfo basic =
                    raw.First(x => x.Name == _context.BaseNormAcid);
                foreach (AnalysisInfo t in raw)
                    t.Norm = t.Counts * 1d / basic.Counts;
            }
        }

        private static void CreateChartsUI(ICollection<Chart> target,
            IEnumerable<RegressionResult> regressionResults)
        {
            target.Clear();
            foreach (RegressionResult rs in regressionResults)
            {
                var (vX, vY) = rs.GetSources();
                var dt = vX.Zip(vY,
                    (x, y) => new KeyValuePair<double, double>(x, y)).ToArray();
                if (!vX.All(double.IsFinite) ||
                    !vY.All(double.IsFinite))
                    continue;
                var chart = new Chart
                {
                    Title = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"{rs.Acid}",
                                HorizontalAlignment =
                                    HorizontalAlignment.Center,
                                FontWeight = FontWeights.Bold,
                                FontSize = 18
                            },
                            new TextBlock
                            {
                                Text = $"R²={rs.R2:F4}, {rs.GetEquation()}",
                                HorizontalAlignment = HorizontalAlignment.Center
                            }
                        }
                    },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    MinHeight = 400,
                    MaxHeight = 1100
                };
                var vmin = vX.Min();
                var vmax = vX.Max();

                var serieTrend = new LineSeries
                {
                    Title = "Trendline",
                    ItemsSource = new[]
                    {
                        new KeyValuePair<double, double>(vmin,
                            rs.A + rs.B * vmin),
                        new KeyValuePair<double, double>(vmax,
                            rs.A + rs.B * vmax)
                    },
                    DependentValuePath = "Value",
                    IndependentValuePath = "Key",
                    IsSelectionEnabled = true
                };
                chart.Series.Add(serieTrend);
                var serie = new LineSeries
                {
                    Title = rs.Acid,
                    DependentValuePath = "Value",
                    IndependentValuePath = "Key",
                    ItemsSource = dt,
                    IsSelectionEnabled = true
                };

                chart.Series.Add(serie);
                target.Add(chart);
            }
        }


        private async Task RunRegressionAnalysis(ICollection<VFADataItem> list,
            ICollection<RegressionResult> target)
        {
            foreach (VFADataItem vfaDataItem in list)
                await vfaDataItem.EnsureDataLoadedAsync();

            // calculate goodness of fit for each acid
            var result = CalculateRegression(list);

            Dispatcher.Invoke(() =>
            {
                target.Clear();
                foreach (RegressionResult regressionResult in result)
                    target.Add(regressionResult);
            });
        }

        private IEnumerable<RegressionResult> CalculateRegression(
            ICollection<VFADataItem> list)
        {
            list = list.Where(x => x.Loaded).ToArray();
            var acids = GetAvailableAcids(list);
            foreach (var acid in acids.Except(new[] { _context.BaseNormAcid }))
            {
                var elems = list.Select(x =>
                    x.AnalysisInfo.First(info => info.Name == acid)).ToArray();
                var vX = elems.Select(x => x.Result).ToArray();
                var vY = elems.Select(x => x.Norm).ToArray();
                var (a, b) = Fit.Line(vX, vY);
                var r_2 = GoodnessOfFit.RSquared(vX.Select(v => a + b * v), vY);
                yield return new RegressionResult(acid, vX, vY, a, b, r_2);
            }
        }

        private IEnumerable<string> GetAvailableAcids(
            IEnumerable<VFADataItem> list)
        {
            var names = list.Where(x => x.Loaded).Select(x =>
                x.AnalysisInfo.Select(v => v.Name)
                    .Where(name => !string.IsNullOrEmpty(name))).ToArray();
            return names.Aggregate((acc, x) => acc.Zip(x, (s, s1) => s));
        }

        private void OnListBoxDropGiveFeedback(object sender,
            GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);
            Mouse.SetCursor(e.Effects.HasFlag(DragDropEffects.Move)
                ? Cursors.Cross
                : Cursors.No);
            e.Handled = true;
        }

        private void DataGrid_AutoGeneratingColumn(object sender,
            DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(decimal) ||
                e.PropertyType == typeof(double) ||
                e.PropertyType == typeof(float))
                ((DataGridTextColumn)e.Column).Binding.StringFormat = "{0:F4}";
        }

        private void previewVFADatatable_AutoGeneratedColumns(object sender,
            EventArgs e)
        {
            if (!(sender is DataGrid grid)) return;
            foreach (PropertyInfo prop in typeof(AnalysisInfo)
                .GetProperties()
                .Where(x => !x.GetCustomAttributes<PrimAttribute>().Any()))
            {
                DataGridColumn col =
                    grid.Columns.FirstOrDefault(x =>
                        x.Header as string == prop.Name);
                if (col != default)
                    col.Visibility = Visibility.Hidden;
            }
        }

        private void PreviewVFADatatable_OnContextMenuOpening(object sender,
            ContextMenuEventArgs e)
        {
            if (!(sender is DataGrid grid)) return;
            var el = (UIElement)e.OriginalSource;
            if (el.FindParent<DataGridColumnHeader>() != default)
            {
                var col = grid.Columns;
                var cm = new ContextMenu();
                foreach (DataGridColumn dataGridColumn in col)
                {
                    var it = new MenuItem
                    {
                        Header = dataGridColumn.Header,
                        Icon = new FontAwesomeIcon
                        {
                            SolidIcon = dataGridColumn.Visibility ==
                                        Visibility.Hidden
                                ? FontAwesomeSolidIcon.EyeSlash
                                : FontAwesomeSolidIcon.Eye
                        }
                    };
                    it.Click += (s, ev) =>
                    {
                        DataGridColumn column = grid.Columns.First(x =>
                            x.Header == ((MenuItem)ev.OriginalSource)
                            .Header);
                        column.Visibility =
                            column.Visibility == Visibility.Hidden
                                ? Visibility.Visible
                                : Visibility.Hidden;
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

        private void RegresionGridContextMenu_OnContextMenuOpening(
            object sender, ContextMenuEventArgs e)
        {
            if (!(sender is DataGrid)) return;
            var el = (UIElement)e.OriginalSource;
            if (el.FindParent<DataGridColumnHeader>() != default)
                e.Handled = true;
        }

        private void SampleAnalysis_AutoGeneratingColumn(object sender,
            DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyDescriptor is PropertyDescriptor d)
                if (d.ComponentType == typeof(AcidViewModel))
                    e.Cancel = true;
        }

        private void SampleAnalysis_AutoGeneratedColumns(object sender, bool applyCellStyle)
        {
            if (!(sender is DataGrid dg)) return;
            var values = dg.ItemsSource.Cast<object>().Take(1);
            foreach (object first in values)
                if (first is AcidViewModel avm)
                {
                    var cellStyle =
                        FindResource("DataGridCellTemplate" +
                                     (applyCellStyle ? "Colored" : "")) as
                            DataTemplate;
                    var names = avm.Values.GetDynamicMemberNames();
                    foreach (var name in names)
                        dg.Columns.Add(new DataGridTemplateColumn
                        {
                            Header = name,
                            CellTemplate = cellStyle
                        });
                }
        }

        private void StandardsList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is ListBox lb)) return;
            var source = _context.StandardsPaths;
            HandleListKeyAction(lb, e, ref source, StandardList);
            if (source == _context.StandardsPaths) return;
            _context.SetStandards(source);
            RefreshItemsAsync(lb);
            RunReanalysisAsync();
        }

        private void RefreshItemsAsync(ListBox lb)
        {
            Task.Run(async () =>
            {
                await Task.Delay(300);
                Dispatcher.Invoke(() => { lb.Items.Refresh(); });
            });
        }


        private void SamplesList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is ListBox lb)) return;
            var source = _context.SamplesPaths;
            HandleListKeyAction(lb, e, ref source, SamplesList);
            if (source == _context.SamplesPaths) return;
            _context.SetSamples(source);
            RunReanalysisAsync();
        }

        private void HandleListKeyAction(ListBox lb, KeyEventArgs e,
            ref string[] source, IList<VFADataItem> target)
        {
            if (e.Key == Key.Delete)
            {
                var except = lb.SelectedItems
                    .Cast<VFADataItem>().ToArray();
                source = source.Except(except.Select(x => x.FileName))
                    .ToArray();
                foreach (VFADataItem item in except)
                    target.Remove(item);
            }
        }

        private void ValidateTableCache(in bool eCanExecute)
        {
            if (!eCanExecute) ModelAnalysisItemsSource.Clear();
        }

        private void ValidateStandardTableCache(in bool eCanExecute)
        {
            if (!eCanExecute)
            {
                StandardsChartItemsSource.Clear();
                StandardRegressionResults.Clear();
            }
        }

        private void ValidateStandardTables(in bool eCanExecute)
        {
            foreach (UIElement element in new UIElement[]
            {
                MainTabControl,
                ChartStackCorpus
            }.Where(x => x != default))
                element.Visibility =
                    eCanExecute ? Visibility.Visible : Visibility.Hidden;
        }

        private void ValidateSampleTables(in bool eCanExecute)
        {
            foreach (UIElement element in new UIElement[]
            {
                SamplePreviewCorpus,
                SampleAnalysisCorpus
            }.Where(x => x != default))
                element.Visibility =
                    eCanExecute ? Visibility.Visible : Visibility.Hidden;
        }

        private void RunReanalysisAsync()
        {
            _ = Task.Run(AnalyzeAll);
        }

        private async Task AnalyzeAll()
        {
            await Task.Delay(300); // allow cell to commit changes
            await RunStandardRegression();
            await RunSampleAnalysis();
        }

        private async void PreviewVFADatatable_OnCellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(300); // allow cell to commit changes
                await AnalyzeAll();
            });
        }

        private void PreviewVFADatatable2_OnCellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            RunReanalysisAsync();
        }


        #region Standards

        private void OnHasStandards()
        {
            if (_context.HasStandards)
            {
                PrepareList(StandardList, _context.StandardsPaths);

                // set settings box
                var first = StandardList.First();
                first.EnsureDataLoadedAsync().Wait();
                AcidStandardBox.ItemsSource =
                    first.AnalysisInfo.Select(x => x.Name)
                        .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                if (ValueCellBrushParameter.AcidNorm != default)
                {
                    // use settings value
                    AcidStandardBox.SelectedItem =
                        _context.BaseNormAcid =
                            ValueCellBrushParameter.AcidNorm;
                }
                else
                {
                    // use context constant
                    AcidStandardBox.SelectedItem =
                        ValueCellBrushParameter.AcidNorm =
                            _context.BaseNormAcid;
                }

                standardsList.SelectedIndex = 0;
                _ = Task.Run(RunStandardRegression);
            }
            else
            {
                ShowError("You should load standards first!");
            }
        }

        private void PrepareList(IList<VFADataItem> target, string[] values,
            int? insertAt = default, bool clear = true, int dir = 0)
        {
            if (clear)
                target.Clear();

            var final = values.Select(x => new VFADataItem(x));
            if (insertAt.HasValue)
            {
                var v = insertAt.Value;
                foreach (VFADataItem vi in final)
                {
                    target.Insert(v, vi);
                    v += dir;
                }
            }
            else
            {
                target.AddRange(final);
            }
        }

        private void LocalResolver(string arg1, Exception x)
        {
            ShowError("Can not load this file:\n" +
                      $"> {x.Message} ({arg1})");
        }

        private bool ValidateListBeforeRegression(ICollection<VFADataItem> list)
        {
            var check = list.Count < 2;
            if (check) ShowError("Regression requires minimum 2 sources");
            Dispatcher.Invoke(() =>
            {
                LoaderGrid.Visibility = Visibility.Collapsed;
            });
            return !check;
        }


        private async void StandardsList_OnSelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (!((sender as ListBox)?.SelectedItem is VFADataItem item))
                return;
            if (!item.Loaded)
                await item.LoadData();
            UpdatePreviewTables();
        }

        #endregion

        #region Samples

        private void OnHasSamples()
        {
            if (_context.HasSamples)
            {
                LoaderGrid.Visibility = Visibility.Visible;
                PrepareList(SamplesList, _context.SamplesPaths);
                samplesList.SelectedIndex = 0;
                MainTabControl.SelectedIndex = 1;
                Task.Run(AnalyzeAll);
            }
            else
            {
                ShowError("You should load samples first!");
            }
        }


        private async void SamplesList_OnSelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (!((sender as ListBox)?.SelectedItem is VFADataItem item))
                return;
            if (!item.Loaded)
                await item.LoadData();
            UpdatePreviewTables();
        }

        private void UpdatePreviewTables()
        {
            Dispatcher.Invoke(() =>
            {
                previewVFADatatable
                    .GetBindingExpression(ItemsControl.ItemsSourceProperty)
                    ?.UpdateTarget();
                previewVFADatatable2
                    .GetBindingExpression(ItemsControl.ItemsSourceProperty)
                    ?.UpdateTarget();
            });
        }

        #endregion

        #region Drag&Drop

        private void OnListBoxDrag(object sender, MouseEventArgs mouseEventArgs)
        {
            if (sender is ListBoxItem draggedItem &&
                mouseEventArgs.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext,
                    DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void OnListBoxDrop(object sender, DragEventArgs e)
        {
            var droppedData =
                e.Data.GetData(typeof(VFADataItem)) as VFADataItem;
            var target = ((ListBoxItem)sender).DataContext as VFADataItem;
            var listBox = ((ListBoxItem)sender).FindParent<ListBox>();

            HandleDropInternal(
                ((ListCollectionView)listBox.ItemsSource).SourceCollection as
                ObservableCollection<VFADataItem>,
                droppedData, target);

            listBox.Items.Refresh();
            RunReanalysisAsync();
        }

        private static void HandleDropInternal<T>(IList<T> list, T droppedData,
            T target)
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

        private void ReplaceFile(IList<VFADataItem> list, string[] fileNames,
            int bindex)
        {
            list.RemoveAt(bindex);
            PrepareList(list, fileNames, bindex, false);
            RunReanalysisAsync();
        }

        private void AddFiles(IList<VFADataItem> list, string[] fileNames,
            int bindex, bool addBefore)
        {
            if (addBefore)
                PrepareList(list, fileNames.Reverse().ToArray(), bindex, false);
            else
                PrepareList(list, fileNames.ToArray(), bindex + 1, false, 1);
            RunReanalysisAsync();
        }

        private void OnFileListScroll(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg =
                    new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = MouseWheelEvent,
                        Source = sender
                    };
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        #endregion

        private void SettingsTabLoaded(object sender, RoutedEventArgs e)
        {
            WarnThreshold.Text = ValueCellBrushParameter.Warning.ToString("f1");
            DangerColorThreshold.Text = ValueCellBrushParameter.Danger.ToString("f1");

            var allowed =
                new[] { "red", "yellow", "orange", "brown" };
            var filter =
                new[] { "light", "greenyellow", "yellowgreen" };
            var names = typeof(Colors)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(x => allowed.Any(cx =>
                  x.Name.Contains(cx, StringComparison.InvariantCultureIgnoreCase)) &&
                  !filter.Any(vx =>
                  x.Name.Contains(vx, StringComparison.InvariantCultureIgnoreCase)));
            var list = names.Select(propertyInfo =>
                new StackPanel
                {
                    Name = "__Selection__" +
                           propertyInfo.GetValue(default)!.ToString()!
                               .Trim('#'),
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new Grid
                        {
                            Background =
                                new SolidColorBrush(
                                    (Color) propertyInfo.GetValue(default)!),
                            Width = 15,
                            Height = 15
                        },
                        new Label {Content = propertyInfo.Name}
                    }
                });
            // ReSharper disable once PossibleMultipleEnumeration
            var list1 = list.ToArray();
            DangerColorBox.ItemsSource = list1.ToArray();
            DangerColorBox.SelectedItem = list1.First(x =>
                x.Name == "__Selection__" +
                ValueCellBrushParameter.DangerColor.ToString().Trim('#'));

            // ReSharper disable once PossibleMultipleEnumeration
            var list2 = list.ToArray();
            WarnColorBox.ItemsSource = list2.ToArray();
            WarnColorBox.SelectedItem = list2.First(x =>
                x.Name == "__Selection__" +
               ValueCellBrushParameter.WarningColor.ToString().Trim('#'));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        private async void AcidStandardChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueCellBrushParameter.AcidNorm =
                _context.BaseNormAcid = AcidStandardBox.SelectedItem.ToString();
            RecalcCounter[nameof(AcidStandardBox)] = true;
            // update preview only
            await CalculateNorm(StandardList);
            await CalculateNorm(SamplesList);
            UpdatePreviewTables();
        }
    }
}