using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AutoVFA.Misc;
using AutoVFA.Models;

namespace AutoVFA.Converters
{
    public class NameToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (!(value is DataGridCell cell))
                return DependencyProperty.UnsetValue;
            var dc = cell.DataContext as AcidViewModel;
            DataGridColumn d = cell.Column;
            if ((double) dc!.Values[(string) d.Header] is var input)
            {
                var threshold = (ValueThresholdConfig) parameter;
                if (input > threshold!.Danger)
                    return new SolidColorBrush(threshold.DangerColor);
                if (input > threshold.Warning) 
                    return new SolidColorBrush(threshold.WarningColor);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}