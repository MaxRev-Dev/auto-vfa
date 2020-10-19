using System;
using AutoVFA.Misc;

namespace AutoVFA.Models
{
    public class RegressionResult
    {
        public string Acid { get; }
        public double A { get; }
        public double B { get; }
        public double R2 { get; }

        public override string ToString()
        {
            return $"{Acid} (Rsqr: {R2:F4}, {GetEquation()})";
        }

        public RegressionResult(string acid, in double a, in double b, in double r2)
        {
            Acid = acid;
            B = b;
            R2 = r2;
            A = a;
        }

        public string GetEquation()
        {
            return $"y = {A:F4} {(B > 0 ? " + " : " - ")} {Math.Abs(B):F4}x";
        }

        public string GetCsv()
        {
            return new[] { this }.ExportToCSV();
        }
    }
}