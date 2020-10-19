using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace AutoVFA.Misc
{
    public static class Extensions
    {
        public static T FindParent<T>(this DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                collection.Add(value);
            }
        }
    }
}