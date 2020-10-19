using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoVFA.Converters
{
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListBoxItem item = (ListBoxItem)value;
            ListBox listView = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
            return "Level " + index + ": ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}