using System;
using System.Windows.Media;

namespace AutoVFA.Misc
{
    [Serializable]
    public class ValueThresholdConfig
    {
        public float Warning { get; set; } = 10;
        public float Danger { get; set; } = 20;
        public Color WarningColor { get; set; } = Colors.Orange;
        public Color DangerColor { get; set; } = Colors.OrangeRed;

        public string AcidNorm { get; set; }
    }
}