using System.IO;
using System.Linq;

namespace AutoVFA.Misc
{
    internal class AnalyzingContext
    {
        public string[] SamplesPaths { get; private set; }
        public string[] StandardsPaths { get; private set; }
        public string BaseNormAcid { get; set; } = "2 ethyl butyric acid";

        public AnalyzingContext()
        {
        }

        public bool HasSamples =>
            SamplesPaths != default && SamplesPaths.Any();

        public bool HasStandards =>
            StandardsPaths != default && StandardsPaths.Any();

        public int DecimalDigits { get; } = 4;
        public string CsvDecimalSeparator { get; } = ".";
        public string CsvDelimiter { get; } = ",";

        public void SetStandards(string[] fileNames, out string[] ignored)
        {
            var final = fileNames.Where(File.Exists).ToArray();
            ignored = fileNames.Except(final).ToArray();
            SetStandards(final);
        }

        public void SetSamples(string[] fileNames, out string[] ignored)
        {
            var final = fileNames.Where(File.Exists).ToArray();
            ignored = fileNames.Except(final).ToArray();
            SetSamples(final);
        }

        public void SetStandards(string[] fileNames)
        {
            StandardsPaths = fileNames;
        }

        public void SetSamples(string[] fileNames)
        {
            SamplesPaths = fileNames;
        }
    }
}