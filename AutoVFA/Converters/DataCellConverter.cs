using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using AutoVFA.Models;

namespace AutoVFA.Converters
{
    public class DataCellConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (!(value is DataGridCell cell)) return value;
            var dc = cell.DataContext as AcidViewModel;
            DataGridColumn d = cell.Column;
            return ((double) dc!.Values[(string) d.Header]).ToString("F4");
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}