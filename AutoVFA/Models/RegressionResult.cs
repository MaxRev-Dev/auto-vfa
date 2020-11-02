using System;
using AutoVFA.Misc;

namespace AutoVFA.Models
{
    public class RegressionResult
    {
        private readonly double[] _x;
        private readonly double[] _y;

        public RegressionResult(string acid, in double[] x, in double[] y,
            in double a, in double b, in double r2)
        {
            _x = x;
            _y = y;
            Acid = acid;
            B = b;
            R2 = r2;
            A = a;
        }

        public string Acid { get; }
        public double A { get; }
        public double B { get; }
        public double R2 { get; }

        public override string ToString()
        {
            return $"{Acid} (Rsqr: {R2:F4}, {GetEquation()})";
        }

        public string GetEquation()
        {
            return $"y={A:F4}{(B > 0 ? "+" : "-")}{Math.Abs(B):F4}x";
        }

        public string GetCsv()
        {
            return new[] {this}.ExportToCSV();
        }

        public (double[] x, double[] y) GetSources()
        {
            return (_x, _y);
        }

        public double Concentration(in double x)
        {
            return (-A + x) / B;
        }
    }
}