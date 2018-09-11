using MathNet.Numerics.Distributions;

namespace Colossus.Statistics
{
    public class PoissonGenerator : MathNetGenerator
    {
        public PoissonGenerator(double lambda, double scale = 1, double offset = 0) : base(new Poisson(lambda, Randomness.Random))
        {
            Scale = scale;
            Offset = offset;
        }
    }
}
