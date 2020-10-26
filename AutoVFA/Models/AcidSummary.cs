using System.Dynamic;

namespace AutoVFA.Models
{
    internal class AcidSummary : DynamicObject
    {
        private readonly ModelGroup _model;
        private readonly string _acid;

        public AcidSummary(ModelGroup model, string acid)
        {
            _model = model;
            _acid = acid;
        }  
        public double CV_Fraction => _model.CVFraction(_acid);
        public double CV_mM => _model.CVmM(_acid);
        public double Average_mM => _model.Avg(_acid); 
        public double Average_Fraction => _model.AvgFractionmM(_acid);
    }
}