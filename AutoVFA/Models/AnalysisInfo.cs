using AutoVFA.Misc;
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
        [Prim]
        public int Counts { get; set; }
        [Prim]
        public double Result { get; set; }
        [Prim]
        public string Name { get; set; }
        [Ignore]
        [Prim]
        public double Norm { get; set; }
    }
}