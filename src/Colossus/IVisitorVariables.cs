using System.Collections.Generic;

namespace Colossus
{
    public interface IVisitorVariable
    {        
        void SetValues(SimulationObject target);
        IEnumerable<VariableKey> ProvidedVariables { get; }
        IList<VariableKey> DependentVariables { get; }
    }
}
