using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoVFA.Parsers
{
    public class VFASummaryParser : ISummaryParser
    {
        private readonly string _filePath;

        public VFASummaryParser(string filePath)
        {
            _filePath = filePath;
        }

        private static string SectionReg = @"---*\s.*?(.*\s?).*\s((?:(?:.|\n))*?)(?:---|\n\W)";
        private static string PairReg = @"(.*?):(\t| )(.*?(?:\n| {3}))";

        private Dictionary<string, object> _result;

        public Dictionary<string, object> ParseFile()
        {
            var options = RegexOptions.Multiline;
            var _streamReader = new StreamReader(File.OpenRead(_filePath));
            var str = _streamReader.ReadToEnd().Replace("\r\n", "\n");
            _result = new Dictionary<string, object>();
            foreach (Match match in Regex.Matches(str, SectionReg, options))
            {
                var pairs = new Dictionary<string, string>();
                var name = match.Groups[1].Value.Trim();
                var valStr = match.Groups[2].Value.Trim();
                foreach (Match valMatch in Regex.Matches(valStr, PairReg, options))
                {
                    pairs.Add(valMatch.Groups[1].Value.Trim(), valMatch.Groups[3].Value.Trim());
                }
                _result.Add(name, pairs.Any() ? (object)pairs : valStr);
            }
            return _result;
        }

        public IEnumerable<AnalysisInfo> ParseTable(string name)
        {
            if (_result == default)
            {
                ParseFile();
            }

            if (_result[name] is string table && !string.IsNullOrEmpty(table))
            {
                table = table.Substring(table.IndexOf('(') + 1);
                var header = table.Substring(0, table.IndexOf(')'));
                var body = table.Substring(table.IndexOf(')') + 1);
                var norm = header + body;
                var csvReader = new CsvHelper.CsvReader(new StringReader(norm),
                    new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        Delimiter = "\t",
                        HasHeaderRecord = true, 
                        MissingFieldFound = null,
                    });
                return csvReader.GetRecords<AnalysisInfo>();
            }

            return Array.Empty<AnalysisInfo>();
        }
    }
}