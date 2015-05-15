using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public static class NoiseHelper
    {
        public static double MultiplicativeNoise(this double d, double scale)
        {
            //return d*((1 - noise) + 2*Randomness.Random.NextDouble()*noise);

            return d + d*(Randomness.Random.NextDouble() - .5)*scale;
        }

        public static double AdditiveNoise(this double d, double scale)
        {
            return d + Randomness.Random.NextDouble()*scale;
        }


        public static double Shift(this double val, double shift, double min = 0, double max = 1)
        {
            val = (val - min + shift) % (max - min);
            if (val < 0)
            {
                val += (max - min);
            }
            val += min;
            return val;
        }


        public static IRandomGenerator Offset(this IRandomGenerator gen, double offset, double? min = null,
            double? max = null, bool round = false)
        {
            return new OffsetGenerator(gen, offset, min: min, max: max, round: round);            
        }               
    }
}
