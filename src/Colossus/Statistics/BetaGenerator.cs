using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;

namespace Colossus.Statistics
{
    public class BetaGenerator : IRandomGenerator
    {
        public double Scale { get; set; }
        public double Offset { get; set; }
        private Beta _beta;


        public BetaGenerator(double a, double b, double scale = 1, double offset = 0)
        {
            Scale = scale;
            Offset = offset;
            _beta = new Beta(a, b, Randomness.Random);
        }

        public double Next()
        {
            return _beta.Sample()*Scale + Offset;            
        }
    }
}
