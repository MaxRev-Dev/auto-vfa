using System.Collections.Generic;
using System.Linq;

namespace AutoVFA.Models
{
    public class SampleAnalysis
    {
        private readonly VFADataItem _target;

        public SampleAnalysis(VFADataItem sample)
        {
            _target = sample;
        }

        public string Name => _target.Name;
        public double Average => Concentrations.Values.Average();
        public double Sum => Concentrations.Values.Sum();
        public double this[string acidName] => Concentrations[acidName];

        public Dictionary<string, double> Concentrations { get; }
            = new Dictionary<string, double>();

        public void SetConcentration(string acidName,
            in double acidConcentration)
        {
            Concentrations[acidName] = acidConcentration;
        }

        public double Prc(string acidName)
        {
            return Concentrations[acidName] / Sum * 100;
        }

        public AnalysisInfo GetSource(string acidName)
        {
            return _target.AnalysisInfo.First(x => x.Name == acidName);
        }
    }
}