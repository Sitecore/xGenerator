using System;

namespace Colossus.Statistics
{
    public class LambdaGenerator : IRandomGenerator
    {
        private readonly Func<double> _generator;

        public LambdaGenerator(Func<double> generator)
        {
            _generator = generator;
        }

        public double Next()
        {
            return _generator();
        }
    }
}
