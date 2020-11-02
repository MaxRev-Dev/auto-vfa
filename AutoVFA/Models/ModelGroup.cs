using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace AutoVFA.Models
{
    public class ModelGroup
    {
        private readonly List<SampleAnalysis> _units =
            new List<SampleAnalysis>();

        public ModelGroup(SampleAnalysis primaryValue)
        {
            Name = primaryValue.Name;
            _units.Add(primaryValue);
        }

        public string Name { get; }

        public void Add(params SampleAnalysis[] values)
        {
            _units.AddRange(values);
        }

        public double Avg(string acidName)
        {
            return _units.Select(x => x[acidName]).Average();
        }

        public double Stdev(string acidName)
        {
            return _units.Select(x => x[acidName]).StandardDeviation();
        }

        public double SummM(int index)
        {
            return _units[index].Sum;
        }

        public double CVmM(string acidName)
        {
            return Stdev(acidName) / Avg(acidName) * 100;
        }

        public double PrcmM(int index, string acidName)
        {
            return _units[index][acidName] / SummM(index) * 100;
        }

        public double AvgFractionmM(string acidName)
        {
            return _units.Select(x => x[acidName] / x.Sum * 100).Average();
        }

        public double CVFraction(string acidName)
        {
            return _units.Select((_, i) => PrcmM(i, acidName))
                .StandardDeviation() / AvgFractionmM(acidName) * 100;
        }
    }
}