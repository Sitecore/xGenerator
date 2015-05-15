using System;

namespace Colossus.Statistics
{
    public class SkewNormalGenerator : IRandomGenerator
    {
        private NormalGenerator _normalGenerator;
        public double Location { get; set; }
        public double Scale { get; set; }
        public double Shape { get; set; }

        public SkewNormalGenerator(double location, double scale, double shape)
        {
            _normalGenerator = new NormalGenerator(0, 1);
            Location = location;
            Scale = scale;
            Shape = shape;
        }

        public double Next()
        {            
            var r = Shape / Math.Sqrt(1 + Shape * Shape);
            var u0 = _normalGenerator.Next();            
            var v = _normalGenerator.Next();
            var u1 = r * u0 + Math.Sqrt(1 - r * r) * v;            
            return (u0 >= 0 ? u1 : -u1) * Scale + Location;
        }
    }
}