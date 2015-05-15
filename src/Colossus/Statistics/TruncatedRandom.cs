using System;

namespace Colossus.Statistics
{
    public class TruncatedRandom : IRandomGenerator
    {

        public static int MaxSamples = 50000;

        public IRandomGenerator Inner { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }        

        public TruncatedRandom(IRandomGenerator inner, double? min = null, double? max = null)
        {
            Inner = inner;
            Min = min;
            Max = max;            
        }

        public double Next()
        {
            var i = 0;
            double value = 0d;
            do
            {
                value = Inner.Next();                

                if ( ++i > MaxSamples) throw new TimeoutException(string.Format("A value within the allowed range was not obtained after {0} samples", MaxSamples));                

            } while ((Min.HasValue && value < Min) || (Max.HasValue && value >= Max));

            
            return value;
        }
    }
}