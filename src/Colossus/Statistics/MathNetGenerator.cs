using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;

namespace Colossus.Statistics
{
    public class MathNetGenerator : IRandomGenerator
    {
        private Func<double> _sampler;

        public double Scale { get; set; }
        public double Offset { get; set; }

        private MathNetGenerator(double scale = 1, double offset = 0)
        {
            Scale = scale;
            Offset = offset;
        }

        public MathNetGenerator(IContinuousDistribution distribution, double scale = 1, double offset = 0)
            :this(scale,offset)
        {
            _sampler = distribution.Sample;            
        }

        public MathNetGenerator(IDiscreteDistribution distribution, double scale = 1, double offset = 0)
            : this(scale, offset)
        {            
            _sampler = ()=>(double)distribution.Sample();
        }

        public double Next()
        {
            return Scale*_sampler() + Offset;
        }
    }
}
