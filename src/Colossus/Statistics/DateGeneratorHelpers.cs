using System;
using Sitecore.Syndication;

namespace Colossus.Statistics
{
    public static class DateGeneratorHelpers
    {
        
        public static DateGenerator Year(this DateGenerator date,            
            Action<TrendBuilder> trend)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(
                min: DateGenerator.DateToYearFraction(date.Start.Value),
                max: DateGenerator.DateToYearFraction(date.End.Value));
            trend(builder);
            
            date.YearGenerator = builder.Build();
            return date;
        }

        public static DateGenerator PartOfYear(this DateGenerator date, Action<TrendBuilder> trend, double weight = 0.5)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(
                min: 0,
                max: 1,
                cyclic: true);

            trend(builder);

            date.YearWeight = 1 - weight;
            date.PartOfYearGenerator = builder.Build();
            return date;
        }

        public static DateGenerator Month(this DateGenerator date, Action<TrendBuilder> trend, double weight = 0.5)
        {
            ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(
                min: 1,
                max: 13,
                cyclic: true);

            trend(builder);

            date.YearWeight = 1 - weight;
            date.MonthGenerator = builder.Build();
            return date;
        }

        public static DateGenerator DayOfWeek(this DateGenerator date, Action<TrendBuilder> trend)
        {
            //ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(
                min: 0,
                max: 7,
                cyclic: true);

            trend(builder);

            date.DayOfWeekGenerator = builder.Build();
            return date;
        }

        public static DateGenerator Hour(this DateGenerator date, Action<TrendBuilder> trend)
        {
            //ValidateDateGeneratorRange(date);

            var builder = new TrendBuilder(
                min: 0,
                max: 24,
                cyclic: true);

            trend(builder);

            date.HourGenerator = builder.Build();
            return date;
        }

        static void ValidateDateGeneratorRange(DateGenerator generator)
        {
            if (!generator.Start.HasValue || !generator.End.HasValue)
            {
                throw new DateRangeNotInitializedException();                   
            }
        }

    }
}
