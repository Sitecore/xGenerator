using System.Collections.Generic;
using System.Linq;

namespace Colossus
{
    public class CompositeVariable : VisitorVariableBase
    {
        public IVisitorVariable Var1 { get; set; }
        public IVisitorVariable Var2 { get; set; }
        public double Var2Prob { get; set; }

        public CompositeVariable(IVisitorVariable var1, IVisitorVariable var2, double var2Prob)
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

        public override IEnumerable<VariableKey> ProvidedVariables => Var1.ProvidedVariables.Concat(Var2.ProvidedVariables).Distinct();

        public IEnumerable<VariableKey> DependendtVariables => Var1.DependentVariables.Concat(Var2.DependentVariables).Distinct();

        protected override bool Equals(VisitorVariableBase other)
        {
            var o = other as CompositeVariable;

            return o != null && o.Var1.Equals(Var1);
        }
    }
}
