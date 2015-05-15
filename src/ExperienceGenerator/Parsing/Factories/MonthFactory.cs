using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Colossus.Statistics;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing.Factories
{
    public class MonthFactory : VariableFactory
    {

        private readonly double[] _partsOfYears;

        public MonthFactory()
        {
            var rd = new DateTime(2014, 1, 1);
            var dates = new List<DateTime>();
            dates.Add(new DateTime(rd.Year - 1, 12, DateTime.DaysInMonth(rd.Year - 1, 12)/2));
            for (var i = 1; i <= 12; i++)
            {
                dates.Add(new DateTime(2014, i, DateTime.DaysInMonth(2014, i)/2));
            }
            dates.Add(new DateTime(rd.Year + 1, 1, DateTime.DaysInMonth(rd.Year - 1, 1) / 2));

            _partsOfYears = dates.Select(d => (d - rd).TotalDays/365d).ToArray();
        }

        public override void UpdateSegment(VisitorSegment segment, JToken definition, XGenParser parser)
        {
            var weights = Enumerable.Range(1, 12).Select(i => 0d).ToArray();
            foreach (var kv in (JObject) definition)
            {
                weights[int.Parse(kv.Key) - 1] = kv.Value.Value<double>();
            }

            segment.DateGenerator.PartOfYear(t =>
            {
                t.Clear();
                t.MoveAbsolute(0, (weights[0] + weights[11])/2);
                for (var i = 0; i < weights.Length; i++)
                {
                    t.LineAbsolute(_partsOfYears[i + 1], weights[i]);
                }
                t.LineAbsolute(1, (weights[0] + weights[11]) / 2);
            });
        }
    }
}

