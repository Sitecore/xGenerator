using System;

namespace Colossus.Statistics
{
    public class LinearGenerator : IRandomGenerator
    {
        private Random _random;
        public double Min { get; set; }
        public double Max { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double Increase { get; set; }

        public LinearGenerator(double min = 0, double max = 1, double start = 1, double end = 1)
        {
            _random = Randomness.Random;
            Min = min;
            Max = max;
            Start = start;
            End = end;
        }


        double Cdf(double t)
        {
            return 0.5 * (End * t * t - Start * t * t) + Start * t;
        }

        double Quantile(double u)
        {
            return (Start - Math.Sqrt(2 * End * u + Start * Start - 2 * Start * u)) / (Start - End);
        }

        public double Next()
        {
            if (Min == Max) return Min;

            if (Start == End)
            {
                return Min + (Max - Min) * _random.NextDouble();
            }

            var u = _random.NextDouble() * (Cdf(1) - Cdf(0));
            return Min + (Max - Min) * Quantile(u);
        }

        public static LinearGenerator Fixed(double value)
        {
            return new LinearGenerator(value, value);
        }
    }
}