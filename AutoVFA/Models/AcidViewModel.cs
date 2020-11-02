using System.Collections.Generic;
using AutoVFA.Misc;

namespace AutoVFA.Models
{
    internal class AcidViewModel
    {
        public AcidViewModel(string name, BindableDynamicDictionary values)
        {
            Name = name;
            Values = values;
        }

        public AcidViewModel(string name, Dictionary<string, AcidSummary> dict2)
        {
            Name = name;
            Sources = dict2;
        }

        public Dictionary<string, AcidSummary> Sources { get; }
        public string Name { get; }
        public BindableDynamicDictionary Values { get; }
    }
}