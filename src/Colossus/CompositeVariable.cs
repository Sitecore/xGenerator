using System.Collections.Generic;
using System.Linq;

namespace Colossus
{
    public class CompositeVariable : VisitorVariablesBase
    {
        public IVisitorVariables Var1 { get; set; }
        public IVisitorVariables Var2 { get; set; }
        public double Var2Prob { get; set; }

        public CompositeVariable(IVisitorVariables var1, IVisitorVariables var2, double var2Prob)
        {
            Var1 = var1;
            Var2 = var2;
            Var2Prob = var2Prob;
        }

        public override void SetValues(SimulationObject target)
        {
            var var = Randomness.Random.NextDouble() < Var2Prob ? Var2 : Var1;
            var.SetValues(target);
        }

        public override IEnumerable<string> ProvidedVariables { get
        {
            return Var1.ProvidedVariables.Concat(Var2.ProvidedVariables).Distinct();
        } }

        public IEnumerable<string> DependendtVariables
        {
            get
            {
                return Var1.DependendtVariables.Concat(Var2.DependendtVariables).Distinct();
            }
        }

        protected override bool Equals(VisitorVariablesBase other)
        {
            var o = other as CompositeVariable;

            return o != null && o.Var1.Equals(Var1);
        }
    }
}
