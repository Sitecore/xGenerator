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

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public double YearWeight { get; set; }

        public DateGenerator(DateTime? startDate = null, DateTime? endDate = null)
        {
            YearWeight = .5d;
            StartDate = startDate;
            EndDate = endDate;
        }


        public DateTime NextDate()
        {
            return StartDate >= EndDate ? DateTime.Now : GetDateInRange(StartDate, EndDate);
        }

        public DateGenerator Clone()
        {
            return (DateGenerator) MemberwiseClone();
        }

        private DateTime GetDateInRange(DateTime? startDate, DateTime? endDate)
        {
            var yearGenerator = YearGenerator;
            if (yearGenerator == null && startDate.HasValue && endDate.HasValue)
            {
                yearGenerator = new LinearGenerator(DateToYearFraction(startDate.Value), DateToYearFraction(endDate.Value));
            }

            DateTime d;
            var i = 0;
            do
            {
                d = GenerateDate(yearGenerator);
                if (++i > TruncatedRandom.MaxSamples)
                    throw new TimeoutException($"A value within the allowed range was not obtained after {TruncatedRandom.MaxSamples} samples");
            } while ((startDate.HasValue && d < startDate) || (endDate.HasValue && d >= endDate));

            return d;
        }

        private DateTime GenerateDate(IRandomGenerator yearGenerator)
        {
            var d = DateTime.Now;

            if (yearGenerator != null)
            {
                d = YearFractionToDate(yearGenerator.Next());
            }


            if (PartOfYearGenerator != null)
            {
                var val = PartOfYearGenerator.Next();
                var days = val * (DateTime.IsLeapYear(d.Year) ? 366 : 365);

                var dnew = new DateTime(d.Year, 1, 1).AddDays(days);
                if (YearGenerator != null && Distance(d, dnew) > 1d)
                    return DateTime.MinValue;
                d = dnew;
            }

            if (MonthGenerator != null)
            {
                var val = MonthGenerator.Next();

                if (YearGenerator != null || PartOfYearGenerator != null)
                {
                    if (d.Month != (int)val)
                    {
                        return DateTime.MinValue;
                    }
                }
                else
                {
                    var days = (d.Day - 1) / (double)DateTime.DaysInMonth(d.Year, d.Month);
                    d = new DateTime(d.Year, (int)val, 1 + (int)(days * DateTime.DaysInMonth(d.Year, (int)val)));
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
         }

        private static double Distance(DateTime d1, DateTime d2)
        {
            var y = DateTime.IsLeapYear(d1.Year) ? 366 : 365;

            AssertDate2LargerOrEqualsDate1(ref d1, ref d2);

            return Math.Min(Math.Abs(d1.DayOfYear - d2.DayOfYear), Math.Abs(d1.DayOfYear - (d2.DayOfYear + y)));
        }

        private static void AssertDate2LargerOrEqualsDate1(ref DateTime d1, ref DateTime d2)
        {
            if (d1.DayOfYear >= d2.DayOfYear)
                return;

            var _ = d2;
            d2 = d1;
            d1 = _;
        }

        public static DateTime YearFractionToDate(double val)
        {
            var y = (int) Math.Floor(val);
            return new DateTime(y, 1, 1).AddDays((val - y)*(DateTime.IsLeapYear(y) ? 366 : 365));
        }

        public static double DateToYearFraction(DateTime date)
        {
            var firstDay = new DateTime(date.Year, 1, 1);
            return date.Year + (date - firstDay).TotalDays/(DateTime.IsLeapYear(date.Year) ? 366 : 365);
        }
    }
}
