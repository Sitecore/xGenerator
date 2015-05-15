using System;
using System.Collections.Generic;

namespace Colossus
{
    public interface IVisitorVariables
    {        
        void SetValues(SimulationObject target);

        IEnumerable<string> ProvidedVariables { get; }
        IEnumerable<string> DependendtVariables { get; }
    }

    public abstract class VisitorVariablesBase : IVisitorVariables
    {
        public abstract void SetValues(SimulationObject target);

        public abstract IEnumerable<string> ProvidedVariables { get; }
        
        public List<string> DependentVariables { get; private set; }

        protected VisitorVariablesBase()
        {
            DependentVariables = new List<string>();
        }

        IEnumerable<string> IVisitorVariables.DependendtVariables
        {
            get { return DependentVariables; }
        }


        protected virtual bool Equals(VisitorVariablesBase other)
        {
            return other.GetType() == GetType();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var other = obj as VisitorVariablesBase;
            
            return other != null ? Equals(other) : false;
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }
    }

    public class SingleVisitorVariable<TValue> : VisitorVariablesBase
    {
        public string Key { get; set; }


        public Func<SimulationObject, TValue> Sampler { get; set; }
        public bool OmitIfNull { get; set; }

        public SingleVisitorVariable(string key, Func<SimulationObject, TValue> sampler, bool omitIfNull = false)
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


        public override IEnumerable<string> ProvidedVariables
        {
            get { yield return Key; }
        }


        protected override bool Equals(VisitorVariablesBase other)
        {
            var o = other as SingleVisitorVariable<TValue>;
            return o != null && Key.Equals(o.Key);
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
