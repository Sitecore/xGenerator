using System;
using System.Collections.Generic;
using System.Linq;

namespace Colossus.Statistics
{
    public class WeightedSetBuilder<TValue>
    {
        private List<KeyValuePair<TValue, double>> _weights;
        public WeightedSetBuilder()
        {
            _weights = new List<KeyValuePair<TValue, double>>();
        }

        public WeightedSetBuilder<TValue> Add(TValue value, double weight = 1d)
        {
            if (weight > 0)
            {
                _weights.Add(new KeyValuePair<TValue, double>(value, weight));
            }
            return this;
        }

        public Func<TValue> Build()
        {
            if (_weights.Count == 0) throw new InvalidOperationException("No items specified");
            if (_weights.Count == 1) return () => _weights[0].Key;

            var set = new WeightedRandom(_weights.Select(w => w.Value));

            return () => _weights[(int)set.Next()].Key;
        }

        public static implicit operator Func<TValue>(WeightedSetBuilder<TValue> builder)
        {
            return builder.Build();
        }
    }
}
