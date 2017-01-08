using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Colossus.Integration.Models;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Parsing.Factories
{
    public class LandingPageFactory : VariableFactory
    {
        public override void UpdateSegment(VisitorSegment segment, JToken definition, XGenParser parser)
        {
            if (definition["Site"] == null || !definition["Site"].Any())
                throw new Exception($"No sites defined.");
            var siteId = parser.ParseWeightedSet<string>(definition["Site"]);

            var landingPage = GetLandingPagesFromDefinition(definition, parser);
            var randomPagePct = GetRandomPagePercentageFromDefinition(definition);
            if (landingPage == null)
            {
                landingPage = () => null;
                randomPagePct = 1d;
            }

            var sitePages = GetSitePages(parser);

            segment.VisitVariables.AddOrReplace(new LandingPageVariable(() =>
                                                                        {
                                                                            var page = parser.InfoClient.GetItemInfo(landingPage());
                                                                            var site = parser.InfoClient.Sites[siteId()];
                                                                            if (page == null || Randomness.Random.NextDouble() < randomPagePct)
                                                                            {
                                                                                return Tuple.Create(site, sitePages[site.Id]());
                                                                            }

                                                                            for (var i = 0; i < 10 && !page.Path.StartsWith(site.StartPath); i++)
                                                                            {
                                                                                site = parser.InfoClient.Sites[siteId()];
                                                                            }

                                                                            return Tuple.Create(site, page);
                                                                        }, parser.InfoClient));
        }

        private double GetRandomPagePercentageFromDefinition(JToken definition)
        {
            return definition.Value<double?>("RandomPagePercentage") ?? 0.2;
        }

        private static Func<string> GetLandingPagesFromDefinition(JToken definition, XGenParser parser)
        {
            if (definition["Item"] == null || !definition["Item"].Any())
                return null;
            return parser.ParseWeightedSet<string>(definition["Item"]);
        }

        private Dictionary<string, Func<ItemInfo>> GetSitePages(XGenParser parser)
        {
            var randomPages = new Dictionary<string, Func<ItemInfo>>();

            foreach (var site in parser.InfoClient.Sites.Values.Where(s => !string.IsNullOrEmpty(s.StartPath)))
            {
                var root = parser.InfoClient.Query(site.StartPath, maxDepth: 4).FirstOrDefault();
                if (root == null)
                {
                    throw new Exception($"Root item for site {site.Id} does not exist ({site.StartPath})");
                }
                var homePct = 0.5;
                if (root.Children.Count == 0)
                    homePct = 1;
                var other = GetSitePages(root).Exponential(0.8, 10);

                randomPages[site.Id] = () => Randomness.Random.NextDouble() < homePct ? root : other();
            }
            return randomPages;
        }

        private IOrderedEnumerable<ItemInfo> GetSitePages(ItemInfo root)
        {
            return GetDescendants(new [] {root}).Select(t => t.Item1).Where(item => item.HasLayout).OrderBy(item => Randomness.Random.NextDouble());
        }

        public override void SetDefaults(VisitorSegment segment, XGenParser parser)
        {
            UpdateSegment(segment, new JObject(), parser);
        }

        private IEnumerable<Tuple<ItemInfo, int>> GetDescendants(IEnumerable<ItemInfo> items, int depth = 1)
        {
            foreach (var item in items)
            {
                yield return Tuple.Create(item, depth);
                foreach (var child in GetDescendants(item.Children, depth + 1))
                {
                    yield return child;
                }
            }
        }

        private class LandingPageVariable : VisitorVariableBase
        {
            private readonly Func<Tuple<SiteInfo, ItemInfo>> _siteAndItem;
            private readonly ItemInfoClient _itemInfo;


            public LandingPageVariable(Func<Tuple<SiteInfo, ItemInfo>> siteAndItem, ItemInfoClient itemInfo)
            {
                _siteAndItem = siteAndItem;
                _itemInfo = itemInfo;

                DependentVariables.Add(VariableKey.Language); //TODO: Language?
                DependentVariables.Add(VariableKey.Campaign);
            }

            public override void SetValues(SimulationObject target)
            {
                var data = _siteAndItem();
                var site = data.Item1;
                var item = data.Item2;

                target.Variables.Add(VariableKey.Site, site.Id);
                if (!string.IsNullOrEmpty(site.Host))
                {
                    target.Variables.Add(VariableKey.Host, "http://" + site.Host.Split('|')[0]);
                }

                var lang = target.GetVariable<string>(VariableKey.Language);
                if (!string.IsNullOrEmpty(lang) && item.Language != lang)
                {
                    item = _itemInfo.GetItemInfo(item.Id.ToString(), lang);
                }

                var url = item.SiteUrls[site.Id];
                if (url.Contains("://"))
                {
                    url = new Uri(url).PathAndQuery;
                }

                var campaignId = target.GetVariable<string>(VariableKey.Campaign);
                if (!string.IsNullOrEmpty(campaignId))
                {
                    url += (url.Contains("?") ? "&" : "?") + "sc_camp=" + campaignId;
                }

                target.Variables.Add(VariableKey.LandingPage, url);
            }

            public override IEnumerable<VariableKey> ProvidedVariables => new[] {VariableKey.Site, VariableKey.Host, VariableKey.LandingPage};
        }
    }
}
