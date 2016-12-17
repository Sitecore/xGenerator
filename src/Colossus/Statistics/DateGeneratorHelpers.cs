using System;

namespace Colossus.Statistics
{
    public static class DateGeneratorHelpers
    {
        public static DateGenerator Year(this DateGenerator date, Action<TrendBuilder> trend)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(DateGenerator.DateToYearFraction(date.StartDate.Value), DateGenerator.DateToYearFraction(date.EndDate.Value));
            trend(builder);

            date.YearGenerator = builder.Build();
            return date;
        }

        public static DateGenerator PartOfYear(this DateGenerator date, Action<TrendBuilder> trend, double weight = 0.5)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(0, 1, cyclic: true);

            trend(builder);

            date.YearWeight = 1 - weight;
            date.PartOfYearGenerator = builder.Build();
            return date;
        }

        public static DateGenerator Month(this DateGenerator date, Action<TrendBuilder> trend, double weight = 0.5)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(1, 13, cyclic: true);

            trend(builder);

            date.YearWeight = 1 - weight;
            date.MonthGenerator = builder.Build();
            return date;
        }

        public static DateGenerator DayOfWeek(this DateGenerator date, Action<TrendBuilder> trend)
        {
            //ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(0, 7, cyclic: true);

            trend(builder);

            date.DayOfWeekGenerator = builder.Build();
            return date;
        }

        public static DateGenerator Hour(this DateGenerator date, Action<TrendBuilder> trend)
        {
            //ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(0, 24, cyclic: true);

            trend(builder);

            date.HourGenerator = builder.Build();
            return date;
        }


        private static void ValidateDateGeneratorRange(DateGenerator generator)
        {
            if (!generator.StartDate.HasValue || !generator.EndDate.HasValue)
            {
                throw new DateRangeNotInitializedException();
            }
        }
    }
}
