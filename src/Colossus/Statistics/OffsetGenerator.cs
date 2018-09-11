using System;

namespace Colossus.Statistics
{
    public class OffsetGenerator : IRandomGenerator
    {
        public double Offset { get; set; }
        public IRandomGenerator Inner { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool Round { get; set; }

        public OffsetGenerator(IRandomGenerator inner, double offset, double? min = null, double? max = null, bool round = false)
        {
            Offset = offset;
            Inner = inner;
            Min = min;
            Max = max;
            Round = round;
        }

        public double Next()
        {            
            var value = Min.HasValue && Max.HasValue
                ? Inner.Next().Shift(Offset, Min.Value, Max.Value)
                : Inner.Next() + Offset;

            if (Round)
            {
                value = Math.Round(value);
            }

            return value;
        }
    }
}
