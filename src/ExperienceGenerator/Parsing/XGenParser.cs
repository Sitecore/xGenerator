using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Colossus;
using Colossus.Integration.Behaviors;
using Colossus.Integration.Models;
using Colossus.Integration.Processing;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;
using ExperienceGenerator.Parsing.Factories;
using ExperienceGenerator.Repositories;
using ExperienceGenerator.Services;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing
{
    public class XGenParser
    {
        private readonly string _sitecoreRoot;
        public static Dictionary<VariableKey, VariableFactory> Factories { get; set; }
        public ItemInfoClient InfoClient { get; set; }
        private readonly GeoDataRepository _geoDataRepository;

        static XGenParser()
        {
            Factories = new Dictionary<VariableKey, VariableFactory>
                        {
                            {VariableKey.PageViews, VariableFactory.Lambda((segment, token, parser) => segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.PageViews, new PoissonGenerator(token.Value<double>()).Truncate(1, 20))))},
                            {VariableKey.VisitCount, VariableFactory.Lambda((segment, token, parser) => segment.VisitorVariables.AddOrReplace(Variables.Random(VariableKey.VisitCount, new PoissonGenerator(token.Value<double>()).Truncate(1, 20))))},
                            {VariableKey.BounceRate, VariableFactory.Lambda((segment, token, parser) => segment.VisitVariables.AddOrReplace(Variables.Boolean(VariableKey.Bounce, token.Value<double>())))},
                            {VariableKey.Duration, VariableFactory.Lambda((segment, token, parser) =>
                                                                          {
                                                                              var mean = token.Value<double>();
                                                                              segment.RequestVariables.AddOrReplace(Variables.Duration(new SkewNormalGenerator(mean, mean, 3)));
                                                                          })},
                            {VariableKey.StartDate, VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.StartDate = token.Value<DateTime>())},
                            {VariableKey.EndDate, VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.EndDate = token.Value<DateTime>())},
                            {VariableKey.DayOfWeek, VariableFactory.Lambda((segment, token, parser) => segment.DateGenerator.DayOfWeek(t => t.Clear().Weighted(builder =>
                                                                                                                                                               {
                                                                                                                                                                   foreach (var kv in (JObject) token)
                                                                                                                                                                   {
                                                                                                                                                                       DayOfWeek day;
                                                                                                                                                                       builder.Add(Enum.TryParse(kv.Key, out day) ? (int) day : int.Parse(kv.Key), kv.Value.Value<double>());
                                                                                                                                                                   }
                                                                                                                                                               })))},
                            {VariableKey.YearlyTrend, VariableFactory.Lambda((segment, token, parser) =>
                                                                             {
                                                                                 if (token.Value<double>() != 1d)
                                                                                 {
                                                                                     segment.DateGenerator.Year(t => t.Clear().MoveAbsolutePercentage(0).LineAbsolutePercentage(1, token.Value<double>()));
                                                                                 }
                                                                             })},
                            {VariableKey.Month, new MonthFactory()},
                            {VariableKey.Identified, VariableFactory.Lambda((segment, token, parser) => segment.VisitorVariables.AddOrReplace(new ContactDataVariable(token.Value<double>())))},
                            {VariableKey.Campaign, VariableFactory.Lambda((segment, token, parser) =>
                                                                          {
                                                                              var campaignPct = token.Value<double?>("Percentage") ?? 1;
                                                                              var campaigns = parser.ParseWeightedSet<string>(token["Weights"]);
                                                                              segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.Campaign, () => Randomness.Random.NextDouble() < campaignPct ? campaigns() : null, true));
                                                                          })},
                            {VariableKey.Channel, VariableFactory.Lambda((segment, token, parser) =>
                                                                         {
                                                                             var channelPct = token.Value<double?>("Percentage") ?? 1;
                                                                             var channels = parser.ParseWeightedSet<string>(token["Weights"]);
                                                                             segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.Channel, () => Randomness.Random.NextDouble() < channelPct ? channels() : null, true));
                                                                         })},
                            {VariableKey.Referrer, VariableFactory.Lambda((segment, token, parser) => segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.Referrer, parser.ParseWeightedSet<string>(token), true)))},
                            {VariableKey.Geo, VariableFactory.Lambda((segment, token, parser) =>
                                                                     {
                                                                         var regionId = parser.ParseWeightedSet<string>(token["Region"]);
                                                                         segment.VisitorVariables.AddOrReplace(new GeoVariables(() => new GetRandomCityService().GetRandomCity(regionId())));
                                                                     })},
                            {VariableKey.Devices, VariableFactory.Lambda((segment, token, parser) =>
                                                                         {
                                                                             Func<string> userAgent;
                                                                             if (!token.HasValues)
                                                                             {
                                                                                 userAgent = new DeviceRepository().GetAll().Select(d => d.UserAgent).Exponential(.8, 8);
                                                                             }
                                                                             else
                                                                             {
                                                                                 var id = parser.ParseWeightedSet<string>(token);
                                                                                 userAgent = () => new DeviceRepository().GetAll().ToDictionary(ga => ga.Id, ga => ga)[id()].UserAgent;
                                                                             }
                                                                             segment.VisitorVariables.AddOrReplace(Variables.Random(VariableKey.UserAgent, userAgent));
                                                                         })},
                            {VariableKey.Outcomes, VariableFactory.Lambda((segment, token, parser) =>
                                                                          {
                                                                              var value = new NormalGenerator(10, 5).Truncate(1);
                                                                              segment.VisitVariables.AddOrReplace(new OutcomeVariable(parser.ParseSet<string>(token), value.Next));
                                                                          })},
                            {VariableKey.InternalSearch, VariableFactory.Lambda((segment, token, parser) =>
                                                                                {
                                                                                    var searchPct = token.Value<double?>("Percentage") ?? 0.2;
                                                                                    var keywords = parser.ParseWeightedSet<string>(token["Keywords"]);
                                                                                    segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.InternalSearch, () => Randomness.Random.NextDouble() < searchPct ? keywords() : null, true));
                                                                                })},
                            {VariableKey.ExternalSearch, VariableFactory.Lambda((segment, token, parser) =>
                                                                                {
                                                                                    var searchPct = token.Value<double?>("Percentage") ?? 0.2;
                                                                                    var keywords = parser.ParseWeightedSet<string>(token["Keywords"]);

                                                                                    var engineId = parser.ParseWeightedSet<string>(token["Engine"]);

                                                                                    segment.VisitVariables.AddOrReplace(new ExternalSearchVariable(() => Randomness.Random.NextDouble() >= searchPct ? null : SearchEngine.SearchEngines.ToDictionary(s => s.Id)[engineId()], () => new[] {keywords()}));
                                                                                })},
                            {VariableKey.Language, VariableFactory.Lambda((segment, token, parser) =>
                                                                          {
                                                                              var languages = parser.ParseWeightedSet<string>(token);
                                                                              segment.VisitVariables.AddOrReplace(Variables.Random(VariableKey.Language, languages));
                                                                          })},
                            {VariableKey.LandingPage, new LandingPageFactory()}
                        };
        }

        public XGenParser(string sitecoreRoot)
        {
            _sitecoreRoot = sitecoreRoot;
            _geoDataRepository = new GeoDataRepository();
            InfoClient = new ItemInfoClient(new Uri(new Uri(sitecoreRoot), "/api/xgen/").ToString());
        }

        public virtual IEnumerable<VisitorSegment> ParseContacts(JToken definition, JobType type)
        {
            var segments = new List<VisitorSegment>();
            if (JobType.Contacts != type)
            {
                return segments;
            }

            foreach (var contact in (JArray) definition)
            {
                if (contact["interactions"] == null)
                    continue;
                foreach (var interactionJObject in contact["interactions"])
                {
                    var segment = new VisitorSegment(contact.Value<string>("email"));
                    var interaction = interactionJObject.ToObject<Interaction>();

                    //set city
                    if (interaction.GeoData != null)
                    {
                        var city = _geoDataRepository.Cities.FirstOrDefault(x => x.GeoNameId == interaction.GeoData.GeoNameId);
                        segment.VisitorVariables.Add(new GeoVariables(() => city));
                    }

                    //set contact
                    segment.VisitVariables.Add(ExtractContact(contact));

                    //set channel (can be overriden below)
                    segment.VisitVariables.Add(new SingleVisitorVariable<string>(VariableKey.Channel, visit => interaction.ChannelId));


                    //set search options
                    if (!string.IsNullOrEmpty(interaction.SearchEngine))
                    {
                        var searchEngine = SearchEngine.SearchEngines.First(s => s.Id.Equals(interaction.SearchEngine, StringComparison.InvariantCultureIgnoreCase));
                        if (!string.IsNullOrEmpty(interaction.SearchKeyword))
                        {
                            var searchKeywords = interaction.SearchKeyword.Split(' ');
                            segment.VisitVariables.AddOrReplace(new ExternalSearchVariable(() => searchEngine, () => searchKeywords));
                        }
                    }
                    //set userAgent
                    SetUserAgent(segment);

                    //set datetime
                    segment.DateGenerator.StartDate = DateTime.Today.AddHours(12).AddMinutes(Randomness.Random.Next(-240, 240)).AddSeconds(Randomness.Random.Next(-240, 240)).Add(TimeSpan.Parse(interaction.Recency));


                    //set outcomes
                    if (interaction.Outcomes != null)
                    {
                        var outcomes = interaction.Outcomes.Select(x => x.Id.ToString());
                        var value = new NormalGenerator(10, 5).Truncate(1);
                        segment.VisitVariables.AddOrReplace(new OutcomeVariable(() => new HashSet<string>(outcomes), value.Next));
                    }

                    var pageItemInfos = interaction.Pages?.ToArray() ?? Enumerable.Empty<PageItemInfo>().ToArray();
                    var pages = new List<PageDefinition>();


                    //set campaign (can be overriden below)
                    if (!string.IsNullOrEmpty(interaction.CampaignId) && pageItemInfos.Any())
                    {
                        var pageItemInfo = pageItemInfos.First();
                        pageItemInfo.Path = pageItemInfo.Path + "?sc_camp=" + interaction.CampaignId;
                    }

                    foreach (var page in pageItemInfos)
                    {
                        var pageDefinition = new PageDefinition
                                             {
                                                 Path = page.Path
                                             };

                        //set goals
                        if (page.Goals != null)
                        {
                            pageDefinition.RequestVariables.Add(VariableKey.TriggerEvents, page.Goals.Select(x => new TriggerEventData
                                                                                                                  {
                                                                                                                      Id = x.Id,
                                                                                                                      Name = x.DisplayName,
                                                                                                                      IsGoal = true
                                                                                                                  }).ToList());
                        }
                        pages.Add(pageDefinition);
                    }

                    var visitorBehavior = new StrictWalk(_sitecoreRoot, pages);
                    segment.Behavior = () => visitorBehavior;
                    segments.Add(segment);
                }
            }
            return segments;
        }

        private static void SetUserAgent(VisitorSegment segment)
        {
            var userAgent = new DeviceRepository().GetAll().Select(d => d.UserAgent).Exponential(.8, 8);
            segment.RequestVariables.Add(Variables.Random(VariableKey.UserAgent, userAgent));
        }

        private static IdentifiedContactDataVariable ExtractContact(JToken contact)
        {
            return new IdentifiedContactDataVariable
                   {
                       Address = contact.Value<string>("address"),
                       BirthDate = contact.Value<string>("birthday"),
                       Email = contact.Value<string>("email"),
                       FirstName = contact.Value<string>("firstName"),
                       MiddleName = contact.Value<string>("middleName"),
                       LastName = contact.Value<string>("lastName"),
                       Gender = contact.Value<string>("gender"),
                       JobTitle = contact.Value<string>("jobTitle"),
                       Picture = contact.Value<string>("image"),
                       Phone = contact.Value<string>("phone")
                   };
        }

        public virtual Func<VisitorSegment> ParseSegments(JToken definition, JobType type)
        {
            if (definition == null || !definition.Any())
            {
                throw new Exception("At least one segment is required");
            }

            var segments = new Dictionary<string, KeyValuePair<VisitorSegment, double>>();

            foreach (var kv in (JObject) definition)
            {
                var segment = new VisitorSegment(kv.Key);
                var def = (JObject) kv.Value;

                segment.DateGenerator.Hour(t => t.AddPeak(0.4, 0.25, 0, pct: true).AddPeak(0.8, 0.1, 2, 0.2, pct: true));
                //SetUserAgent(segment);


                if (type != JobType.Contacts)
                {
                    segment.VisitorVariables.Add(Variables.Random(VariableKey.VisitCount, new PoissonGenerator(3).Truncate(1, 10)));
                    segment.VisitorVariables.Add(Variables.Random(VariableKey.PageViews, new PoissonGenerator(3).Truncate(1, 10)));
                    segment.VisitVariables.Add(Variables.Random(VariableKey.Pause, new NormalGenerator(7, 7).Truncate(0.25, 1)));
                }

                var visitorBehavior = new RandomWalk(_sitecoreRoot);
                segment.Behavior = () => visitorBehavior;

                segments.Add(kv.Key, new KeyValuePair<VisitorSegment, double>(segment, def.Value<double?>("Weight") ?? 1d));

                var copy = def["Copy"];
                if (copy != null)
                {
                    foreach (var name in copy.Values<string>())
                    {
                        segment.Copy(segments[name].Key);
                    }
                }

                var usedFactories = new HashSet<VariableKey>();

                foreach (var prop in def)
                {
                    if (prop.Key == "Weight" || prop.Key == "Copy")
                    {
                        continue;
                    }

                    var variableKey = ParseKey(prop.Key);
                    if (!variableKey.HasValue)
                        throw new Exception($"Variable key '{prop.Key}' not registered.");

                    try
                    {
                        Factories[variableKey.Value].UpdateSegment(segment, prop.Value, this);
                        usedFactories.Add(variableKey.Value);
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new Exception($"No factory registered for {variableKey}");
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

        private VariableKey? ParseKey(string stringValue)
        {
            VariableKey key;
            if (!Enum.TryParse(stringValue, true, out key))
                return null;
            return key;
        }

        public Func<ISet<TValue>> ParseSet<TValue>(JToken token)
        {
            var set = (JObject) token;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));

            var probs = new Dictionary<TValue, double>();

            foreach (var kv in set)
            {
                var key = (TValue) converter.ConvertFromString(kv.Key);
                var value = kv.Value.Value<double>();
                if (key != null)
                {
                    probs.Add(key, value);
                }
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

            var set = (JObject) token;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));

            var hasWeight = false;
            var builder = new WeightedSetBuilder<TValue>();
            foreach (var kv in set)
            {
                var weight = kv.Value.Value<double>();
                if (weight > 0)
                {
                    hasWeight = true;
                    builder.Add(kv.Key == "" ? default(TValue) : (TValue) converter.ConvertFromString(kv.Key), weight);
                }
            }

            return hasWeight ? builder.Build() : () => default(TValue);
        }
    }
}
