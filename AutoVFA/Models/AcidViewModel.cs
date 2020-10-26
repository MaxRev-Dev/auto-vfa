using AutoVFA.Misc;

namespace AutoVFA.Models
{
    internal class AcidViewModel
    {
        public string Name { get; }
        public BindableDynamicDictionary Values { get; }

        public AcidViewModel(string name, BindableDynamicDictionary values)
        {
            Name = name;
            Values = values;
        }
    }
}