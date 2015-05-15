using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Statistics
{
    public class DateRangeNotInitializedException : ApplicationException
    {
        public DateRangeNotInitializedException() : base("Date range not initialized")
        {
            
        }
    }
}
