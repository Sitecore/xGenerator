namespace ExperienceGenerator.Parsing
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using Colossus;
  using Colossus.Integration;
  using Colossus.Statistics;
  using ExperienceGenerator.Data;
  using ExperienceGenerator.Parsing.Factories;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  public class XGenParser
  {
    private readonly string _sitecoreRoot;

    public static Dictionary<string, VariableFactory> Factories { get; set; }


    public static GeoData GeoData { get; }


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
        segment.RequestVariables.AddOrReplace(Variables.Duration(new SkewNormalGenerator(mean, mean, 3), 1));
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
        var value = new NormalGenerator(10, 5).Truncate(1);
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
          () => new[]
          {
            keywords()
          }));
      }));


      Factories.Add("LandingPage", new LandingPageFactory());
    }

    public XGenParser(string sitecoreRoot)
    {
      this._sitecoreRoot = sitecoreRoot;
      this.InfoClient = new ItemInfoClient(new Uri(new Uri(sitecoreRoot), "/api/xgen/").ToString());
    }

    public virtual IEnumerable<VisitorSegment> ParseContacts(JToken definition, JobType type)
    {
      var segments = new List<VisitorSegment>();
      if (JobType.Contacts == type)
      {
        foreach (var contact in (JArray)definition)
        {
          if (contact["interactions"] == null)
            continue;
          foreach (var interaction in contact["interactions"])
          {

            var segment = new VisitorSegment(contact.Value<string>("email"));


            //set city
            var city = interaction["geoData"].ToObject<City>();
            city = GeoData.Cities.Find(x=>x.GeoNameId == city.GeoNameId);
            segment.VisitorVariables.Add(new GeoVariables(() => city));
            //set contact
            segment.VisitVariables.Add(new IdentifiedContactDataVariable(contact.Value<string>("email"), contact.Value<string>("firstName"), contact.Value<string>("lastName")));

            //set channel (can be overriden below)
            var channelId = interaction.Value<string>("channelId");
            segment.VisitVariables.Add(new SingleVisitorVariable<string>("Channel",(visit)=>channelId));

            //set search options
            var engine = interaction.Value<string>("searchEngine");
            if (engine != null)
            {
              var searchEngine = SearchEngine.SearchEngines.First(s => s.Id.Equals(engine, StringComparison.InvariantCultureIgnoreCase));
              var keyword = interaction.Value<string>("searchKeyword");
              if (keyword != null)
              {
                var searchKeywords = keyword.Split(new char[] { ' ' });
                segment.VisitVariables.AddOrReplace(new ExternalSearchVariable(() => searchEngine, () => searchKeywords));

              }
            }


            //set datetime
            segment.DateGenerator.Start = DateTime.Now.Add(TimeSpan.Parse(interaction.Value<string>("recency")));


            segments.Add(segment);
            var visitorBehavior = new StrictWalk(this._sitecoreRoot, interaction["pages"].Select(x => x.Value<string>("path")));
            segment.Behavior = () => visitorBehavior;
          }

        }
      }
      return segments;

    }
    public virtual Func<VisitorSegment> ParseSegments(JToken definition, JobType type)
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
        this.InitializeSegment(segment, def, type);

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
          if (prop.Key == "Weight" || prop.Key == "Copy")
          {
            continue;
          }

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

        foreach (var factory in Factories.Where(factory => !usedFactories.Contains(factory.Key)))
        {
          factory.Value.SetDefaults(segment, this);
        }

        segment.SortVariables();

      }

      return segments.Values.Weighted();
    }


    protected virtual void InitializeSegment(VisitorSegment segment, JObject definition, JobType type)
    {
      segment.DateGenerator.Hour(t => t.AddPeak(0.4, 0.25, 0, pct: true)
          .AddPeak(0.8, 0.1, 2, 0.2, pct: true));
      var userAgent = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.useragents.txt")
         .Exponential(.8, 8);
      segment.RequestVariables.Add(Variables.Random("UserAgent", userAgent));


      if (type != JobType.Contacts)
      {
        segment.VisitorVariables.Add(Variables.Random("VisitCount", new PoissonGenerator(3).Truncate(1, 10)));
        segment.VisitorVariables.Add(Variables.Random("PageViews", new PoissonGenerator(3).Truncate(1, 10)));
        segment.VisitVariables.Add(Variables.Random("Pause", new NormalGenerator(7, 7).Truncate(0.25)));
      }

      var visitorBehavior = new RandomWalk(this._sitecoreRoot);
      segment.Behavior = () => visitorBehavior;
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
          if (Randomness.Random.NextDouble() < prob.Value)
          {
            sample.Add(prob.Key);
          }
        }
        return sample;
      };
    }

    public Func<TValue> ParseWeightedSet<TValue>(JToken token)
    {
      if (token == null || !token.Any())
      {
        return () => default(TValue);
      }

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