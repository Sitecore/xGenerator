namespace Colossus.Statistics
{
    public class WignerGenerator : IRandomGenerator
    {
        public double R { get; set; }
        public double Shift { get; set; }

        private IRandomGenerator _beta;

        public WignerGenerator(double r, double shift = 0d)
        {
            R = r;
            Shift = shift;
            _beta = new BetaGenerator(1.5, 1.5);
        }

        public double Next()
        {
            return 2 * R * _beta.Next() - R + Shift;
        }
    }
}
