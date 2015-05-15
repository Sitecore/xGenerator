using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colossus
{
    public class Randomness
    {
        private static ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());

        public static Random Random { get { return _random.Value; } }        

        public static void Seed(int seed)
        {
            _random.Value = new Random(seed);
        }
    }
}
