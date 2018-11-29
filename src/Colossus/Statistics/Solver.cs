using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public static class Solver
    {
        private const int MaxIterations = 10000;
        private const double Eps = 1e-8;


        //Stole this one from Wikipedia
        public static double BrentsMethodSolve(Func<double, double> function, double lowerLimit, double upperLimit,
            double errorTol = Eps)
        {
            double a = lowerLimit;
            double b = upperLimit;
            double c = 0;
            double d = double.MaxValue;

            double fa = function(a);
            double fb = function(b);

            double fc = 0;
            double s = 0;
            double fs = 0;

            // if f(a) f(b) >= 0 then error-exit
            if (fa * fb >= 0)
            {
                if (fa < fb)
                    return a;
                else
                    return b;
            }

            // if |f(a)| < |f(b)| then swap (a,b) end if
            if (Math.Abs(fa) < Math.Abs(fb))
            {
                double tmp = a;
                a = b;
                b = tmp;
                tmp = fa;
                fa = fb;
                fb = tmp;
            }

            c = a;
            fc = fa;
            bool mflag = true;
            int i = 0;

            while (!(fb == 0) && (Math.Abs(a - b) > errorTol))
            {
                if ((fa != fc) && (fb != fc))
                    // Inverse quadratic interpolation
                    s = a * fb * fc / (fa - fb) / (fa - fc) + b * fa * fc / (fb - fa) / (fb - fc) + c * fa * fb / (fc - fa) / (fc - fb);
                else
                    // Secant Rule
                    s = b - fb * (b - a) / (fb - fa);

                double tmp2 = (3 * a + b) / 4;
                if ((!(((s > tmp2) && (s < b)) || ((s < tmp2) && (s > b)))) ||
                    (mflag && (Math.Abs(s - b) >= (Math.Abs(b - c) / 2))) ||
                    (!mflag && (Math.Abs(s - b) >= (Math.Abs(c - d) / 2))))
                {
                    s = (a + b) / 2;
                    mflag = true;
                }
                else
                {
                    if ((mflag && (Math.Abs(b - c) < errorTol)) || (!mflag && (Math.Abs(c - d) < errorTol)))
                    {
                        s = (a + b) / 2;
                        mflag = true;
                    }
                    else
                        mflag = false;
                }
                fs = function(s);
                d = c;
                c = b;
                fc = fb;
                if (fa * fs < 0)
                {
                    b = s;
                    fb = fs;
                }
                else
                {
                    a = s;
                    fa = fs;
                }

                // if |f(a)| < |f(b)| then swap (a,b) end if
                if (Math.Abs(fa) < Math.Abs(fb))
                {
                    double tmp = a;
                    a = b;
                    b = tmp;
                    tmp = fa;
                    fa = fb;
                    fb = tmp;
                }
                i++;
                if (i > MaxIterations)
                    throw new Exception(String.Format("Error is {0}", fb));
            }

            return b;
        }
    }
}
