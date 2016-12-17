using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;

namespace Colossus
{
    public static class CollectionHelpers
    {
        public static T GetOrDefault<TKey, T>(this IDictionary<TKey, T> dict, TKey key, T defaultValue = default(T))
        {
            T v;
            return dict.TryGetValue(key, out v) ? v : defaultValue;
        }

        public static void AddOrReplace(this IList<IVisitorVariable> vars, IVisitorVariable value, double blend = 0)
        {
            var currentIndex = vars.IndexOf(value);
            if (currentIndex > -1)
            {
                vars[currentIndex] = blend > 0 ? new CompositeVariable(vars[currentIndex], value, blend) : value;
                return;
            }

            vars.Add(value);
        }

        public static T GetVariable<T>(this SimulationObject obj, VariableKey key, T defaultValue = default(T))
        {
            var v = obj as Visit;
            if (v != null)
                return v.GetVariable(key, defaultValue);
            var r = obj as Request;
            if (r != null)
                return r.GetVariable(key, defaultValue);

            return (T) obj.Variables.GetOrDefault(key, defaultValue);
        }

        public static T GetVariable<T>(this Visit obj, VariableKey key, T defaultValue = default(T))
        {
            return (T) obj.Variables.GetOrDefault(key, obj.Visitor.GetVariable(key, defaultValue));
        }

        public static T GetVariable<T>(this Request obj, VariableKey key, T defaultValue = default(T))
        {
            return (T) obj.Variables.GetOrDefault(key, obj.Visit.GetVariable(key, defaultValue));
        }

        public static VisitorSegment BackgroundVariables(this VisitorSegment segment, params IVisitorVariable[] variables)
        {
            return segment.BackgroundVariables((IEnumerable<IVisitorVariable>) variables);
        }

        public static VisitorSegment VisitVariables(this VisitorSegment segment, params IVisitorVariable[] variables)
        {
            segment.VisitVariables.AddRange(variables);

            return segment;
        }

        public static VisitorSegment RequestVariables(this VisitorSegment segment, params IVisitorVariable[] variables)
        {
            segment.RequestVariables.AddRange(variables);

            return segment;
        }

        public static VisitorSegment BackgroundVariables(this VisitorSegment segment, IEnumerable<IVisitorVariable> variables)
        {
            segment.VisitorVariables.AddRange(variables);
            return segment;
        }

        public static VisitorSegment StartDateTime(this VisitorSegment segment, DateTime start, DateTime end, Action<DateGenerator> date = null)
        {
            segment.DateGenerator.StartDate = start;
            segment.DateGenerator.EndDate = end;

            date?.Invoke(segment.DateGenerator);

            return segment;
        }

        public static VisitorSegment Copy(this VisitorSegment segment, VisitorSegment other)
        {
            segment.DateGenerator = other.DateGenerator.Clone();
            segment.VisitorVariables.AddRange(other.VisitorVariables);
            segment.VisitVariables.AddRange(other.VisitVariables);
            segment.RequestVariables.AddRange(other.RequestVariables);
            segment.Behavior = other.Behavior;

            return segment;
        }


        public static IRandomGenerator Truncate(this IRandomGenerator gen, double? min = null, double? max = null)
        {
            return new TruncatedRandom(gen, min, max);
        }


        internal static IDictionary<VariableKey, object> GetSerializableVariables(this IDictionary<VariableKey, object> vars)
        {
            return vars.Where(v => !typeof(MulticastDelegate).IsAssignableFrom(v.GetType().BaseType)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static List<T> TopologicalSort<T>(this IEnumerable<T> values, Func<T, ISet<T>, bool> dependencyCheck)
        {
            var workingSet = new HashSet<T>(values);
            var sorted = new List<T>(workingSet.Count);
            while (workingSet.Count > 0)
            {
                var free = workingSet.FirstOrDefault(x => !dependencyCheck(x, workingSet));
                if (free == null)
                {
                    throw new InvalidOperationException("Cyclic dependency detected");
                }
                sorted.Add(free);
                workingSet.Remove(free);
            }

            return sorted;
        }

        public static bool DependsOn(this IVisitorVariable var, IVisitorVariable other)
        {
            return !var.Equals(other) && other.ProvidedVariables.Any(p => var.DependentVariables.Contains(p));
        }
    }
}
