using System;

namespace Colossus.Statistics
{
    public class DateGenerator
    {
        public DateTime ReferenceDate { get; set; }
        public IRandomGenerator YearGenerator { get; set; }
        public IRandomGenerator PartOfYearGenerator { get; set; }
        public IRandomGenerator MonthGenerator { get; set; }
        public IRandomGenerator DayOfWeekGenerator { get; set; }
        public IRandomGenerator HourGenerator { get; set; }

        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public double YearWeight { get; set; }

        public DateGenerator(
            DateTime? start = null,
            DateTime? end = null)
        {
            YearWeight = .5d;
            Start = start;
            End = end;
        }

        public DateTime NextDate()
        {
            var d = DateTime.Now;

            if (Start >= End) return d;


            var yearGenerator = YearGenerator;

            if (yearGenerator == null && Start.HasValue && End.HasValue)
            {
                yearGenerator = new LinearGenerator(DateToYearFraction(Start.Value), DateToYearFraction(End.Value));
            }

            d = GetInRange(() =>
            {
                if (yearGenerator != null)
                {
                    d = YearFractionToDate(yearGenerator.Next());
                }


                if (PartOfYearGenerator != null)
                {
                    //if (YearGenerator == null || Randomness.Random.NextDouble() < 1 - YearWeight)
                    {
                        //                        d = YearFractionToDate(_uniform.Next());
                        var val = PartOfYearGenerator.Next();
                        var days = val * (DateTime.IsLeapYear(d.Year) ? 366 : 365);

                        var dnew = new DateTime(d.Year, 1, 1).AddDays(days);
                        if (YearGenerator != null && Distance(d, dnew) > 1d) return DateTime.MinValue;
                        d = dnew;
                    }
                }

                if (MonthGenerator != null)
                {
                    //if (YearGenerator == null || Randomness.Random.NextDouble() < 1 - YearWeight)
                    {
                        var val = MonthGenerator.Next();

                        if (YearGenerator != null || PartOfYearGenerator != null)
                        {
                            if (d.Month != (int) val)
                            {
                                return DateTime.MinValue;
                                //var daysInMonth = DateTime.DaysInMonth(d.Year, (int)val);
                                //var mid = new DateTime(d.Year, (int) val, daysInMonth/2);
                                //var p = Distance(d, mid)/(double)daysInMonth;                                
                                //if (Randomness.Random.NextDouble() < p) return DateTime.MinValue;
                            }
                        }
                        else
                        {
                            var days = (d.Day - 1)/(double) (DateTime.DaysInMonth(d.Year, d.Month));
                            d = new DateTime(d.Year, (int) val, 1 + (int) (days*DateTime.DaysInMonth(d.Year, (int) val)));
                        }
                    }
                }

                if (DayOfWeekGenerator != null)
                {

                    var val = (int)DayOfWeekGenerator.Next();
                    d = d.AddDays(-(int)d.DayOfWeek + val);
                }

                if (HourGenerator != null)
                {
                    d = d.Date.AddHours(HourGenerator.Next());
                }

                return d;
            });

            return d;
        }

        public DateGenerator Clone()
        {
            return (DateGenerator)MemberwiseClone();
        }

        private DateTime GetInRange(Func<DateTime> setter)
        {
            DateTime d;
            var i = 0;
            do
            {
                d = setter();
                if (++i > TruncatedRandom.MaxSamples) throw new TimeoutException(string.Format("A value within the allowed range was not obtained after {0} samples", TruncatedRandom.MaxSamples));
            } while ((Start.HasValue && d < Start) || (End.HasValue && d >= End));

            return d;
        }

        static double Distance(DateTime d1, DateTime d2)
        {
            var y = DateTime.IsLeapYear(d1.Year) ? 366 : 365;

            if (d1.DayOfYear < d2.DayOfYear)
            {
                var _ = d2;
                d2 = d1;
                d1 = _;
            }

            return Math.Min(Math.Abs(d1.DayOfYear - d2.DayOfYear), Math.Abs(d1.DayOfYear - (d2.DayOfYear + y)));
        }

        public static DateTime YearFractionToDate(double val)
        {
            var y = (int)Math.Floor(val);
            return new DateTime(y, 1, 1).AddDays((val - y) * (DateTime.IsLeapYear(y) ? 366 : 365));
        }

        public static double DateToYearFraction(DateTime date)
        {
            var firstDay = new DateTime(date.Year, 1, 1);
            return date.Year + (date - firstDay).TotalDays / (DateTime.IsLeapYear(date.Year) ? 366 : 365);
        }
    }
}
