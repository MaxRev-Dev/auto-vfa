using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace AutoVFA.Misc
{
    public static class DataExporter
    {
        public static string ExportToCSV<T>(this IEnumerable<T> items)
        {
            var cfg = new CsvConfiguration(CultureInfo.CurrentUICulture);
            cfg.AutoMap<T>();

            using var writer = new StringWriter();
            using var csvWriter = new CsvWriter(writer, cfg);
            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();
            foreach (T item in items)
            {
                csvWriter.WriteRecord(item);
                csvWriter.NextRecord();
            }

            return writer.ToString();
        }

        public static async Task ExportToCSVFileAsync<T>(this IEnumerable<T> items, string fileName)
        {
            var cfg = new CsvConfiguration(CultureInfo.CurrentUICulture);
            cfg.AutoMap<T>();
            if (File.Exists(fileName)) File.Delete(fileName);
            await using var file = File.OpenWrite(fileName);
            await using var streamWriter = new StreamWriter(file);
            await using var csvWriter = new CsvWriter(streamWriter, cfg);
            csvWriter.WriteHeader<T>();
            await csvWriter.NextRecordAsync();
            foreach (T item in items)
            {
                csvWriter.WriteRecord(item);
                await csvWriter.NextRecordAsync();
            }

            await csvWriter.FlushAsync();
        }
    }
}