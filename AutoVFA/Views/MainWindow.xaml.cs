using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AutoVFA.Misc;
using AutoVFA.Models;
using AutoVFA.Parsers;

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
        }

        private string[] _standardsPaths;

        private string[] _samplesPaths;

        public string BaseNormAcid { get; set; } = "2 ethyl butyric acid";

        public static ObservableCollection<VFADataItem> StandardList { get; set; } = new ObservableCollection<VFADataItem>();
        public static ObservableCollection<VFADataItem> SamplesList { get; set; } = new ObservableCollection<VFADataItem>();
        public static ObservableCollection<RegressionResult> RegressionResults { get; set; } = new ObservableCollection<RegressionResult>();

        #region Standards

        private void OnHasStandards()
        {
            if (HasStandards)
            {
                StandardList.Clear();
                StandardList.AddRange(_standardsPaths.Select(x => new VFADataItem(x)));
                RunRegression();
            }
            else
            {
                ShowError("You should load standards first!");
            }
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

        private void ShowError(string errMsg)
        {
            MessageBox.Show(errMsg, "Something happend...",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region Samples


        private void OnHasSamples()
        {
            if (HasSamples)
            {
                SamplesList.Clear();
                SamplesList.AddRange(
                    _samplesPaths.Select(x => new VFADataItem(x)));
            }
            else
            {
                ShowError("You should load samples first!");
            }
        }

        #endregion

        public bool HasSamples => _samplesPaths != default && _samplesPaths.Any();
        public bool HasStandards => _standardsPaths != default && _standardsPaths.Any();

        private void SetGridData()
        {
            if (standardsList.SelectedItem == default) return;
            dataGrid.ItemsSource = ((VFADataItem)standardsList.SelectedItem).AnalysisInfo;
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
                yield return new RegressionResult(acid, a, b, r_2);
            }
        }

        private IEnumerable<string> GetAvailableAcids(ICollection<VFADataItem> list)
        {
            var names = list.Select(x =>
                x.AnalysisInfo.Select(v => v.Name)
                    .Where(name => !string.IsNullOrEmpty(name))).ToArray();
            return names.Aggregate((acc, x) => acc.Zip(x, (s, s1) => s));
        } 
    }
}
