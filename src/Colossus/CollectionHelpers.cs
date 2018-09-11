using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;

namespace Colossus
{
    public static class CollectionHelpers
    {
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            TValue v;
            return dict.TryGetValue(key, out v) ? v : defaultValue;
        }

        public static void AddOrReplace(this IList<IVisitorVariables> vars, IVisitorVariables value, double blend = 0)
        {
            var currentIndex = vars.IndexOf(value);            
            if (currentIndex > -1)
            {
                vars[currentIndex] = blend > 0 ? new CompositeVariable(vars[currentIndex], value, blend) : value;
                return;
            }

            vars.Add(value);
        }

        public static TValue GetVariable<TValue>(this SimulationObject obj, string key,
            TValue defaultValue = default(TValue))
        {
            var v = obj as Visit;
            if (v != null) return v.GetVariable(key, defaultValue);
            var r = obj as Request;
            if (r != null) return r.GetVariable(key, defaultValue);

            return (TValue)obj.Variables.GetOrDefault(key, defaultValue);
        }

        public static TValue GetVariable<TValue>(this Visit obj, string key,
            TValue defaultValue = default(TValue))
        {
            return (TValue)obj.Variables.GetOrDefault(key, obj.Visitor.GetVariable(key, defaultValue));
        }

        public static TValue GetVariable<TValue>(this Request obj, string key,
            TValue defaultValue = default(TValue))
        {
            return (TValue)obj.Variables.GetOrDefault(key, obj.Visit.GetVariable(key, defaultValue));
        }

        public static VisitorSegment BackgroundVariables(this VisitorSegment segment, params IVisitorVariables[] variables)
        {
            return segment.BackgroundVariables((IEnumerable<IVisitorVariables>)variables);
        }

        public static VisitorSegment VisitVariables(this VisitorSegment segment, params IVisitorVariables[] variables)
        {
            segment.VisitVariables.AddRange(variables);

            return segment;
        }

        public static VisitorSegment RequestVariables(this VisitorSegment segment, params IVisitorVariables[] variables)
        {
            segment.RequestVariables.AddRange(variables);

            return segment;
        }

        public static VisitorSegment BackgroundVariables(this VisitorSegment segment, IEnumerable<IVisitorVariables> variables)
        {
            segment.VisitorVariables.AddRange(variables);
            return segment;
        }

        public static VisitorSegment StartDateTime(this VisitorSegment segment,
            DateTime start,
            DateTime end,
            Action<DateGenerator> date = null)
        {
            segment.DateGenerator.Start = start;
            segment.DateGenerator.End = end;

            if (date != null)
            {
                date(segment.DateGenerator);
            }
            
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


        internal static IDictionary<string, object> GetSerializableVariables(this IDictionary<string, object> vars)
        {
            return
                vars.Where(v => !typeof(MulticastDelegate).IsAssignableFrom(v.GetType().BaseType))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static List<TValue> TopologicalSort<TValue>(this IEnumerable<TValue> values, Func<TValue, ISet<TValue>, bool> dependencyCheck)
        {
            var workingSet = new HashSet<TValue>(values);
            var sorted = new List<TValue>(workingSet.Count);
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

        public static bool DependsOn(this IVisitorVariables var, IVisitorVariables other)
        {
            if (var.Equals(other)) return false;

            return other.ProvidedVariables.Any(p => var.DependendtVariables.Contains(p));
        }
    }
}
