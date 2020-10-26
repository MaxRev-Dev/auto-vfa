using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AutoVFA.Misc;
using AutoVFA.Models;

namespace AutoVFA.Converters
{
    public class NameToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is DataGridCell cell)) return DependencyProperty.UnsetValue;
            var dc = cell.DataContext as AcidViewModel;
            var d = cell.Column;
            if ((double)dc!.Values[(string)d.Header] is var input)
            {
                var threshold = (CVThreshold)parameter;
                if (input > threshold!.Danger)
                {
                    return threshold.DangerBrush;
                }

                if (input > threshold.Warning)
                {
                    return threshold.WarningBrush;
                }

            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}