using System;
using System.Windows.Media;

namespace AutoVFA.Misc
{
    [Serializable]
    public class CVThreshold
    {
        public float Warning { get; } = 10;
        public float Danger { get; } = 20;
        public Brush WarningBrush { get; } = Brushes.Orange;
        public Brush DangerBrush { get; } = Brushes.OrangeRed;
    }
}