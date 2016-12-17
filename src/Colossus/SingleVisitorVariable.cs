using System;
using System.Collections.Generic;

namespace Colossus
{
    public class SingleVisitorVariable<TValue> : VisitorVariableBase
    {
        public VariableKey Key { get; set; }

        public Func<SimulationObject, TValue> Sampler { get; set; }
        public bool OmitIfNull { get; set; }

        public SingleVisitorVariable(VariableKey key, Func<SimulationObject, TValue> sampler, bool omitIfNull = false)
        {
            Key = key;
            Sampler = sampler;
            OmitIfNull = omitIfNull;            
        }        

        public override void SetValues(SimulationObject target)
        {
            var value = Sampler(target);
            if (!OmitIfNull || !Equals(value, default(TValue)))
            {
                target.Variables[Key] = Sampler(target);
            }
        }


        public override IEnumerable<VariableKey> ProvidedVariables
        {
            get { yield return Key; }
        }


        protected override bool Equals(VisitorVariableBase other)
        {
            var o = other as SingleVisitorVariable<TValue>;
            return o != null && Key.Equals(o.Key);
        }

        public override string ToString()
        {
            return Key.ToString("G");
        }
    }
}
