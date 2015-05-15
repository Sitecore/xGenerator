using System;

namespace Colossus.Statistics
{
    public class TransformedRandom : IRandomGenerator
    {
        public IRandomGenerator Generator { get; set; }
        public Func<double, double> Transformation { get; set; }

        public TransformedRandom(IRandomGenerator generator, Func<double, double> transformation)
        {
            Generator = generator;
            Transformation = transformation;
        }

        public double Next()
        {
            return Transformation(Generator.Next());
        }
    }
}