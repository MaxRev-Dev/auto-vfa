using CsvHelper.Configuration.Attributes;

namespace AutoVFA.Models
{
    public class AnalysisInfo
    {
        public float tR { get; set; }
        public float Timeoffset { get; set; }
        public float RRT { get; set; }
        public string Sepcode { get; set; }
        public float Width { get; set; }
        public int Counts { get; set; }
        public double Result { get; set; }
        public string Name { get; set; }
        [Ignore]
        public double Norm { get; set; }
    }
}