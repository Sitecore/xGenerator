using System;

namespace Colossus.Statistics
{
    public class DateRangeNotInitializedException : ApplicationException
    {
        public DateRangeNotInitializedException() : base("Date range not initialized")
        {
            
        }
    }
}
