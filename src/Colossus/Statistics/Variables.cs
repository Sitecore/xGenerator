using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public static class Variables
    {
        public static IVisitorVariables Fixed<TValue>(string key, TValue value)
        {
            return Random(key, () => value);
        }
        
        public static IVisitorVariables Random<TValue>(string key, Func<TValue> value, bool omitIfNull = false)
        {
            return new SingleVisitorVariable<TValue>(key, dict=>value(), omitIfNull);
        }

        public static IVisitorVariables Random(string key, IRandomGenerator generator)
        {        
            return Random(key, generator.Next);
        }

        public static IVisitorVariables Dynamic<TValue>(string key, Func<SimulationObject, TValue> value, IEnumerable<string> dependsOn = null)
        {
            var var = new SingleVisitorVariable<TValue>(key, value);
            if (dependsOn != null)
            {
                var.DependentVariables.AddRange(dependsOn);
            }
            return var;
        }

        public static IVisitorVariables Boolean(string key, double probability)
        {
            return Random(key, () => Randomness.Random.NextDouble() < probability);
        }

        public static IVisitorVariables TimeSpan(string key, IRandomGenerator seconds, double? min = 0d, double? max = null)
        {
            seconds = seconds.Truncate(min, max);
            return Random(key, () => System.TimeSpan.FromSeconds(seconds.Next()));
        }

        public static IVisitorVariables Duration(IRandomGenerator seconds, double? min = 1d, double? max = null)
        {
            return TimeSpan("Duration", seconds, min, max);
        }

        public static IVisitorVariables Pause(IRandomGenerator seconds, double? min = 0d, double? max = null)
        {
            return TimeSpan("Pause", seconds, min, max);
        }
    }
}
