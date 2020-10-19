using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper.Configuration;

namespace AutoVFA.Misc
{
    public static class CsvExporter
    {
        public static string ExportToCSV<T>(this IEnumerable<T> items)
        {
            var cfg = new CsvConfiguration(CultureInfo.CurrentUICulture);
            cfg.AutoMap<T>();

            using var writer = new StringWriter();
            using var csvWriter = new CsvHelper.CsvWriter(writer, cfg);
            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();
            foreach (var item in items)
            {
                csvWriter.WriteRecord(item);
                csvWriter.NextRecord();
            }
            return writer.ToString();
        }
    }
}