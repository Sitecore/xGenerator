using System;
using System.Collections.Generic;
using System.Linq;

namespace Colossus.Statistics
{
    public class WeightedRandom : IRandomGenerator
    {
        private readonly double[] _weights;
        private double _totalWeight;

        public WeightedRandom(IEnumerable<double> weights)
        {            
            _weights = weights.Select(w => _totalWeight += w).ToArray();
        }

        public double Next()
        {
            var n = Randomness.Random.NextDouble() * _totalWeight;
            for (var i = 0; i < _weights.Length; i++)
            {
                if (n < _weights[i]) return i;
            }
            
            throw new Exception("All weights can't be 0");
        }
    }
}
