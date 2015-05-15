using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Colossus;
using Colossus.Integration;
using Colossus.Statistics;
using Newtonsoft.Json.Linq;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;
using ExperienceGenerator.Parsing.Factories;

namespace ExperienceGenerator.Parsing
{
    public class XGenParser
    {
        private readonly string _sitecoreRoot;

        public static Dictionary<string, VariableFactory> Factories { get; set; }


        public static GeoData GeoData { get; private set; }


        public ItemInfoClient InfoClient { get; set; }


        static XGenParser()
        {
            GeoData = GeoData.FromResource();

            Factories = new Dictionary<string, VariableFactory>();

            Factories.Add("PageViews", VariableFactory.Lambda((segment, token, parser) =>
                segment.VisitVariables.AddOrReplace(Variables.Random("PageViews",
                    new PoissonGenerator(token.Value<double>()).Truncate(1, 20)))));

            Factories.Add("VisitCount", VariableFactory.Lambda((segment, token, parser) =>
                segment.VisitorVariables.AddOrReplace(Variables.Random("VisitCount",
                    new PoissonGenerator(token.Value<double>()).Truncate(1, 20)))));

            Factories.Add("BounceRate", VariableFactory.Lambda((segment, token, parser) =>
                segment.VisitVariables.AddOrReplace(Variables.Boolean("Bounce", token.Value<double>()))));

            Factories.Add("Duration", VariableFactory.Lambda((segment, token, parser) =>
            {
                var mean = token.Value<double>();
                segment.RequestVariables.AddOrReplace(Variables.Duration(new SkewNormalGenerator(mean, mean, 3), min: 1));
            }));


            Factories.Add("StartDate", VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.Start = token.Value<DateTime>()));
            Factories.Add("EndDate", VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.End = token.Value<DateTime>()));


            Factories.Add("DayOfWeek", VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.DayOfWeek(t =>
                t.Clear().Weighted(builder =>
                {
                    foreach (var kv in (JObject)token)
                    {
                        DayOfWeek day;
                        builder.Add(
                            Enum.TryParse(kv.Key, out day) ? (int)day :
                            int.Parse(kv.Key), kv.Value.Value<double>());
                    }
                })
                )));

            Factories.Add("YearlyTrend", VariableFactory.Lambda((segment, token, parser) =>
            {
                if (token.Value<double>() != 1)
                {
                    segment.DateGenerator.Year(
                        t => t.Clear().MoveAbsolutePercentage(0).LineAbsolutePercentage(1, token.Value<double>()));
                }
                //segment.DateGenerator.YearWeight = 1;
            }));

            Factories.Add("Month", new MonthFactory());

            Factories.Add("Identified",
                VariableFactory.Lambda((segment, token, parser) =>
                    segment.VisitorVariables.AddOrReplace(
                        new ContactDataVariable(token.Value<double>()))));


            Factories.Add("Campaign", VariableFactory.Lambda((segment, token, parser) =>
            {
                var campaignPct = token.Value<double?>("Percentage") ?? 1;
                var campaigns = parser.ParseWeightedSet<string>(token["Weights"]);
                segment.VisitVariables.AddOrReplace(Variables.Random("Campaign",
                    () => Randomness.Random.NextDouble() < campaignPct ? campaigns() : null, true));
            }));


            Factories.Add("Channel", VariableFactory.Lambda((segment, token, parser) =>
            {
                var channelPct = token.Value<double?>("Percentage") ?? 1;
                var channels = parser.ParseWeightedSet<string>(token["Weights"]);
                segment.VisitVariables.AddOrReplace(Variables.Random("Channel",
                    () => Randomness.Random.NextDouble() < channelPct ? channels() : null, true));
            }));


            Factories.Add("Referrer", VariableFactory.Lambda((segment, token, parser) =>
                segment.VisitVariables.AddOrReplace(Variables.Random("Referrer",
                    parser.ParseWeightedSet<string>(token), true))));

            var areas = GeoArea.Areas.ToDictionary(ga => ga.Id, ga => ga.Selector(GeoData));
            Factories.Add("Geo", VariableFactory.Lambda((segment, token, parser) =>
            {
                var regionId = parser.ParseWeightedSet<string>(token["Region"]);
                segment.VisitorVariables.AddOrReplace(new GeoVariables(() => areas[regionId()](), true));
            }));

            Factories.Add("Outcomes", VariableFactory.Lambda((segment, token, parser) =>
            {
                var value = new NormalGenerator(10, 5).Truncate(min: 1);
                segment.VisitVariables.AddOrReplace(new OutcomeVariable(parser.ParseSet<string>(token), value.Next));
            }));

            Factories.Add("InternalSearch", VariableFactory.Lambda((segment, token, parser) =>
            {
                var searchPct = token.Value<double?>("Percentage") ?? 0.2;
                var keywords = parser.ParseWeightedSet<string>(token["Keywords"]);
                segment.VisitVariables.AddOrReplace(Variables.Random("InternalSearch",
                    () => Randomness.Random.NextDouble() < searchPct ? keywords() : null, true));
            }));

            var searchEngines = SearchEngine.SearchEngines.ToDictionary(s => s.Id);
            Factories.Add("ExternalSearch", VariableFactory.Lambda((segment, token, parser) =>
            {
                var searchPct = token.Value<double?>("Percentage") ?? 0.2;
                var keywords = parser.ParseWeightedSet<string>(token["Keywords"]);

                var engineId = parser.ParseWeightedSet<string>(token["Engine"]);

                segment.VisitVariables.AddOrReplace(new ExternalSearchVariable(
                    () =>
                        Randomness.Random.NextDouble() >= searchPct
                            ? null
                            : searchEngines[engineId()],
                    () => new[] { keywords() }));
            }));



            Factories.Add("LandingPage", new LandingPageFactory());
        }

        public XGenParser(string sitecoreRoot)
        {
            _sitecoreRoot = sitecoreRoot;
            InfoClient = new ItemInfoClient(new Uri(new Uri(sitecoreRoot), "/api/xgen/").ToString());
        }


        public virtual Func<VisitorSegment> ParseSegments(JToken definition)
        {
            if (definition == null || !definition.Any())
            {
                throw new Exception("At least one segment is required");
            }

            var segments = new Dictionary<string, KeyValuePair<VisitorSegment, double>>();

            foreach (var kv in (JObject)definition)
            {
                var segment = new VisitorSegment(kv.Key);
                var def = (JObject)kv.Value;
                InitializeSegment(segment, def);

                segments.Add(kv.Key, new KeyValuePair<VisitorSegment, double>(segment, def.Value<double?>("Weight") ?? 1d));

                var copy = def["Copy"];
                if (copy != null)
                {
                    foreach (var name in copy.Values<string>())
                    {
                        segment.Copy(segments[name].Key);
                    }
                }

                var usedFactories = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var prop in def)
                {
                    if (prop.Key != "Weight" && prop.Key != "Copy")
                    {
                        try
                        {
                            Factories[prop.Key].UpdateSegment(segment, prop.Value, this);
                            usedFactories.Add(prop.Key);
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new Exception(string.Format("No factory registered for {0}", prop.Key));
                        }
                    }
                }

                foreach (var factory in Factories)
                {
                    if (!usedFactories.Contains(factory.Key))
                    {
                        factory.Value.SetDefaults(segment, this);
                    }
                }

                segment.SortVariables();

                //Console.Out.WriteLine(segment.Name);
                //foreach (var var in segment.VisitVariables)
                //{
                //    Console.Out.WriteLine(var.ToString());
                //}
                //Console.Out.WriteLine();
            }

            return segments.Values.Weighted();
        }



        protected virtual void InitializeSegment(VisitorSegment segment, JObject definition)
        {
            segment.DateGenerator.Hour(t => t.AddPeak(0.4, 0.25, 0, pct: true)
                .AddPeak(0.8, 0.1, shape: 2, weight: 0.2, pct: true));

            segment.VisitorVariables.Add(Variables.Random("VisitCount", new PoissonGenerator(3).Truncate(1, 10)));
            segment.VisitVariables.Add(Variables.Random("Pause", new NormalGenerator(7, 7).Truncate(0.25)));

            var userAgent = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.useragents.txt")
                .Exponential(.8, 8);
            segment.RequestVariables.Add(Variables.Random("UserAgent", userAgent));

            var randomWalk = new RandomWalk(_sitecoreRoot);
            segment.Behavior = () => randomWalk;
        }



        public Func<ISet<TValue>> ParseSet<TValue>(JToken token)
        {
            var set = (JObject)token;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));

            var probs = new Dictionary<TValue, double>();

            foreach (var kv in set)
            {
                probs.Add((TValue)converter.ConvertFromString(kv.Key), kv.Value.Value<double>());
            }

            return () =>
            {
                var sample = new HashSet<TValue>();
                foreach (var prob in probs)
                {
                    if (prob.Value < Randomness.Random.Next())
                    {
                        sample.Add(prob.Key);
                    }
                }
                return sample;
            };

        }

        public Func<TValue> ParseWeightedSet<TValue>(JToken token)
        {
            if (token == null || !token.Any()) return () => default(TValue);

            var set = (JObject)token;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));

            var hasWeight = false;
            var builder = new WeightedSetBuilder<TValue>();
            foreach (var kv in set)
            {
                var weight = kv.Value.Value<double>();
                if (weight > 0)
                {
                    hasWeight = true;
                    builder.Add(
                        kv.Key == ""
                            ? default(TValue)
                            : (TValue)converter.ConvertFromString(kv.Key), weight);
                }
            }

            return hasWeight ? builder.Build() : () => default(TValue);
        }

    }
}
