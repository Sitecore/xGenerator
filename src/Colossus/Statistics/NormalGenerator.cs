using System;

namespace Colossus.Statistics
{
    public class NormalGenerator : IRandomGenerator
    {
        
        public double Location { get; set; }
        public double Scale { get; set; }

        public NormalGenerator(double location, double scale)
        {            
            Location = location;
            Scale = scale;
        }

        public double Next()
        {
            var random = Randomness.Random;
            var u1 = 1*random.NextDouble();
            var u2 = 1*random.NextDouble();
            
            return Location + Scale * Math.Sqrt(-2 * Math.Log(Math.Max(0.0001,u1))) * Math.Cos(2 * Math.PI * u2);           
        }
    }
}
