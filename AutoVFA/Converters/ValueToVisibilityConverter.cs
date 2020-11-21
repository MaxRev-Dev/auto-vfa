using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoVFA.Converters
{
    public class ValueToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value != default)
            {
                if (value is IDictionary d)
                {
                    if (d.Count > 0)
                        return Visibility.Visible;
                }

                if (value is int v)
                {
                    if (v > 0)
                        return Visibility.Visible;
                }

                if (value is bool b && b)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}