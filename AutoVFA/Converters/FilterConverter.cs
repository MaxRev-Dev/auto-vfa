using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using AutoVFA.Models;

namespace AutoVFA.Converters
{
    public class FilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value is AnalysisInfo[] array)
                return array.Where(x => !string.IsNullOrEmpty(x.Name))
                    .ToArray();
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}