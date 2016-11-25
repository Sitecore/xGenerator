using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.RootFinding;
using Sitecore.Shell.Framework.Commands.TemplateBuilder;

namespace Colossus.Statistics
{   
    public static class Sets
    {        
        public static Func<TValue> Weighted<TValue>(Action<WeightedSetBuilder<TValue>> values)
        {
            var builder = new WeightedSetBuilder<TValue>();
            values(builder);
            return builder.Build();
        }


        public static Func<TValue> Weighted<TValue>(this IEnumerable<KeyValuePair<TValue, int>> values)
        {
          var builder = new WeightedSetBuilder<TValue>();
          foreach (var kv in values)
          {
            builder.Add(kv.Key, kv.Value);
          }
          return builder.Build();
        }

        public static Func<TValue> Weighted<TValue>(this IEnumerable<KeyValuePair<TValue, double>> values)
        {
            var builder = new WeightedSetBuilder<TValue>();
            foreach (var kv in values)
            {
                builder.Add(kv.Key, kv.Value);
            }            
            return builder.Build();
        }

        public static Func<int> WeightedInts(this IEnumerable<double> weights)
        {
            var w = weights.ToArray();
            return Enumerable.Range(0, w.Length).Weighted(w);
        }

        public static IEnumerable<double> AsFunnelWeights(this IEnumerable<double> continueRates)
        {
            var n = 1d;
            foreach (var r in continueRates)
            {
                yield return n;
                n *= r;
            }
            yield return n;
        } 
        
        public static Func<TItem> Blend<TItem>(this IEnumerable<Func<TItem>> distributions, Func<int, double> weights)
        {
            var set = distributions.Weighted((d, i) => weights(i));

            return () => set()();
        }

        public static Func<TItem> Blend<TItem>(this Func<TItem> distribution, Func<TItem> other, double weight = 0.5)
        {
            return () => Randomness.Random.NextDouble() < weight ? other() : distribution();
        }

        public static IEnumerable<TItem> Scramble<TItem>(this IEnumerable<TItem> items, int randomSeed = 1337)
        {
            var r = new Random(randomSeed);
            return items.OrderBy(i => r.NextDouble());
        }

        public static Func<TItem> Weighted<TItem>(this IEnumerable<TItem> items, double[] weights)
        {
            return Weighted(items, (item, index) => weights[index]);
        }
        
        public static Func<TItem> Weighted<TItem>(this IEnumerable<TItem> items, Func<TItem, double> weight)
        {
            return Weighted(items, (item, index) => weight(item));
        }

        public static Func<TItem> Weighted<TItem>(this IEnumerable<TItem> items, Func<TItem, int, double> weight)
        {
            var builder = new WeightedSetBuilder<TItem>();
            var i = 0;
            foreach (var item in items)
            {
                builder.Add(item, weight(item, i++));
            }            
            return builder.Build();
        }

        public static Func<TValue> Distributed<TValue>(IRandomGenerator dist,
            params TValue[] values)
        {
            return () =>
            {
                var index = dist.Next();
                if (index < 0 || index >= values.Length)
                {
                     throw new ArgumentOutOfRangeException(nameof(dist), "The value returned by the random distribution is out of range for the values");
                }

                return values[(int) index];
            };
        }

        public static Func<TValue> Uniform<TValue>(this TValue[] values)
        {
            return ()=>values[Randomness.Random.Next(0, values.Length)];
        }

        
        public static Func<TValue> Exponential<TValue>(this IEnumerable<TValue> values, double topPercent, int index)
        {
            var vs = values.ToArray();
            return Distributed(ExponentialGenerator.TopPerecentage(topPercent, index, vs.Length), vs);
        }

        public static Func<TValue> Pareto<TValue>(this IEnumerable<TValue> values, double topPerecent, int index)
        {
            var vs = values.ToArray();
            return Distributed(ParetoGenerator.TopPercentage(topPerecent, index, maxValue: vs.Length), vs);
        }       
    }


}
