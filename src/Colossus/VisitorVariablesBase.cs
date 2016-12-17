using System.Collections.Generic;

namespace Colossus
{
    public abstract class VisitorVariableBase : IVisitorVariable
    {
        public abstract void SetValues(SimulationObject target);

        public abstract IEnumerable<VariableKey> ProvidedVariables { get; }
        
        public IList<VariableKey> DependentVariables { get; }

        protected VisitorVariableBase()
        {
            DependentVariables = new List<VariableKey>();
        }

        protected virtual bool Equals(VisitorVariableBase other)
        {
            return other.GetType() == GetType();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var other = obj as VisitorVariableBase;
            
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }
    }
}
