using System;

namespace Colossus.Statistics
{
    public class ExponentialGenerator : IRandomGenerator
    {
        private Random _random;
        public double Lambda { get; set; }
        public double? MaxValue { get; set; }        

        public ExponentialGenerator(double lambda, double? maxValue = null)
        {
            _random = Randomness.Random;
            Lambda = lambda;
            MaxValue = maxValue;
        }

        public static double Cdf(double x, double lambda)
        {
            return 1 - Math.Exp(-lambda * x);
        }

        public static double Quantile(double u, double lambda)
        {
            return -Math.Log(1 - u) / lambda;
        }

        public double Next()
        {
            return Quantile((MaxValue.HasValue ? Cdf(MaxValue.Value, Lambda) : 1) * _random.NextDouble(), Lambda);
        }

        public static ExponentialGenerator TopPerecentage(double topPct, double belowValue, double? maxValue = null)
        {
            var lambda = -Math.Log(1 - topPct) / belowValue;
            if (maxValue.HasValue)
            {
                //Can't find closed form expression for alpha. Nor can http://www.wolframalpha.com/ 
                lambda =
                    Solver.BrentsMethodSolve(
                        l => Cdf(belowValue, l) / Cdf(maxValue.Value, l) - topPct, 1e-5, lambda);
            }
            return new ExponentialGenerator(lambda, maxValue);
        }
    }
}