using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public static class Variables
    {
        public static IVisitorVariable Fixed<TValue>(VariableKey key, TValue value)
        {
            return Random(key, () => value);
        }
        
        public static IVisitorVariable Random<TValue>(VariableKey key, Func<TValue> value, bool omitIfNull = false)
        {
            return new SingleVisitorVariable<TValue>(key, dict=>value(), omitIfNull);
        }

        public static IVisitorVariable Random(VariableKey key, IRandomGenerator generator)
        {        
            return Random(key, generator.Next);
        }

        public static IVisitorVariable Dynamic<TValue>(VariableKey key, Func<SimulationObject, TValue> value, IEnumerable<VariableKey> dependsOn = null)
        {
            var var = new SingleVisitorVariable<TValue>(key, value);
            if (dependsOn == null)
                return var;
            foreach (var variable in dependsOn)
            {
                var.DependentVariables.Add(variable);
            }
            return var;
        }

        public static IVisitorVariable Boolean(VariableKey key, double probability)
        {
            return Random(key, () => Randomness.Random.NextDouble() < probability);
        }

        public static IVisitorVariable Duration(IRandomGenerator seconds)
        {
            var generator = seconds.Truncate(1d, 60d);
            return Random(VariableKey.Duration, () => TimeSpan.FromSeconds(generator.Next()));
        }
    }
}
