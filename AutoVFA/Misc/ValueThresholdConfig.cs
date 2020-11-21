using System;
using System.Windows.Media;

namespace AutoVFA.Misc
{
    [Serializable]
    public class ValueThresholdConfig
    {
        public float Warning { get; set; } = 10;
        public float Danger { get; set; } = 20;
        public Brush WarningBrush { get; set; } = Brushes.Orange;
        public Brush DangerBrush { get; set; } = Brushes.OrangeRed;
    }
}