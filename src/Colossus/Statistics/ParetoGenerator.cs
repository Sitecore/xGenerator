using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public class ParetoGenerator : IRandomGenerator
    {
        private Random _random;
        public double Alpha { get; set; }
        public bool ZeroIndexed { get; set; }
        public double? MaxValue { get; set; }

        public ParetoGenerator(double alpha, bool zeroIndexed = true, double? maxValue = null)
        {
            _random = Randomness.Random;
            Alpha = alpha;
            ZeroIndexed = zeroIndexed;
            MaxValue = maxValue;
        }

        public static double Cdf(double x, double alpha, bool zeroIndexed)
        {
            return 1 - Math.Pow(x + (zeroIndexed ? 1 : 0), -alpha);
        }

        public static double Quantile(double u, double alpha, bool zeroIndexed)
        {
            return Math.Pow(1 - u, -1 / alpha) - (zeroIndexed ? 1 : 0);
        }

        public double Next()
        {
            return Quantile((MaxValue.HasValue ? Cdf(MaxValue.Value, Alpha, ZeroIndexed) : 1)
                * _random.NextDouble(), Alpha, ZeroIndexed);
        }

        public static ParetoGenerator TopPercentage(double topPct, double belowValue, bool zeroIndexed = true, double? maxValue = null)
        {
            var alpha = Math.Log(1 / (1 - topPct)) / Math.Log(belowValue + (zeroIndexed ? 1 : 0));

            if (maxValue.HasValue)
            {
                //Can't find closed form expression for alpha. Nor can http://www.wolframalpha.com/ 
                alpha =
                    Solver.BrentsMethodSolve(
                        a => Cdf(belowValue, a, zeroIndexed) / Cdf(maxValue.Value, a, zeroIndexed) - topPct, 1e-5, alpha);
            }

            return new ParetoGenerator(alpha, zeroIndexed, maxValue);
        }
    }
}
