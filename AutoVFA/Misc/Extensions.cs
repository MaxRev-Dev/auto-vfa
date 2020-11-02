using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutoVFA.Misc
{
    public static class Extensions
    {
        public static object FromBase64(string value)
        {
            using var ms = new MemoryStream(Convert.FromBase64String(value));
            var bf = new BinaryFormatter();
            return bf.Deserialize(ms);
        }

        public static string ToBase64(object obj)
        {
            using var ms = new MemoryStream();
            var bf = new BinaryFormatter();
            bf.Serialize(ms, obj);
            ms.Position = 0;
            var buffer = new byte[(int) ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            return Convert.ToBase64String(buffer);
        }

        public static T FindParent<T>(this DependencyObject dependencyObject)
            where T : DependencyObject
        {
            DependencyObject parent =
                VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }

        public static ScrollViewer GetScrollViewer(this UIElement element)
        {
            if (element == null) return null;

            ScrollViewer retour = null;
            for (var i = 0;
                i < VisualTreeHelper.GetChildrenCount(element) &&
                retour == null;
                i++)
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                    retour =
                        (ScrollViewer) VisualTreeHelper.GetChild(element, i);
                else
                    retour = GetScrollViewer(
                        VisualTreeHelper.GetChild(element, i) as UIElement);
            return retour;
        }

        public static void AddRange<T>(this ICollection<T> collection,
            IEnumerable<T> values)
        {
            foreach (T value in values) collection.Add(value);
        }

        /// <summary>
        ///     Groups file with it's versions using Damerau-Levenshtein distance. e.g. xyz [xyz0a,xy0b]
        /// </summary>
        public static Dictionary<string, string[]> GetSimilarFileNames(
            IEnumerable<string> filePaths)
        {
            string normalize(string name)
            {
                return name.Replace(" ", "");
            }

            var fs = filePaths.ToArray();
            var map = new Dictionary<string, string[]>();

            for (var i = 0; i < fs.Length; i++)
            {
                var file = normalize(Path.GetFileName(fs[i]));
                if (map.Values.Any(x => x.Any(s => s.EndsWith(fs[i]))))
                    continue;
                var dist = new Dictionary<string, int>();
                for (var j = 0; j < fs.Length; j++)
                {
                    if (i == j) continue;
                    var file2 = normalize(Path.GetFileName(fs[j]));
                    if (file == file2 ||
                        file!.Length > file2!.Length ||
                        !Path.GetFileNameWithoutExtension(file2)
                            .StartsWith(Path.GetFileNameWithoutExtension(file)))
                        continue;
                    var d = DamerauLevenshteinDistance.Compute(file, file2);
                    if (d > 1 && d < file!.Length) dist[fs[j]] = d;
                }

                if (!dist.Any())
                {
                    map[fs[i]] = Array.Empty<string>();
                    continue;
                }

                var min = dist.Min(x => x.Value);
                if (min > 4)
                    continue;
                var targets = dist.Where(x => x.Value == min);
                map[fs[i]] = targets
                    .Select(v => fs.First(x => x.EndsWith(v.Key))).ToArray();
            }

            return map;
        }
    }
}