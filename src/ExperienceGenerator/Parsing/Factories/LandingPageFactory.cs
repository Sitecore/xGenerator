using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus;
using Colossus.Integration;
using Colossus.Statistics;
using Newtonsoft.Json.Linq;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;

namespace ExperienceGenerator.Parsing.Factories
{
  using Colossus.Integration.Models;

  public class LandingPageFactory : VariableFactory
    {
        public override void UpdateSegment(VisitorSegment segment, JToken definition, XGenParser parser)
        {
            var randomPagePct = definition.Value<double?>("RandomPagePercentage") ?? 0.2;

            Func<string> siteId = () => "website";
            if (definition["Site"] != null && definition["Site"].Any())
            {
                siteId = parser.ParseWeightedSet<string>(definition["Site"]);
            }

            Func<string> landingPage = () => null;
            if (definition["Item"] != null && definition["Item"].Any())
            {
                landingPage = parser.ParseWeightedSet<string>(definition["Item"]);
            }
            else
            {
                randomPagePct = 1d;
            }

            var randomPages = new Dictionary<string, Func<ItemInfo>>();

            foreach (var site in parser.InfoClient.Sites.Values
                .Where(s => !string.IsNullOrEmpty(s.StartPath)))
            {
                var root = parser.InfoClient.Query(site.StartPath, maxDepth: null).FirstOrDefault();
                if (root == null)
                {
                    throw new Exception(string.Format("Root item for site {0} does not exist ({1})", site.Id, site.StartPath));
                }
                var homePct = 0.5;
                if (root.Children.Count == 0) homePct = 1;
                var other = GetDescendants(root.Children).Select(t => t.Item1)
                    .Where(item => item.HasLayout).OrderBy(item => Randomness.Random.NextDouble())
                    .Exponential(0.8, 10);

                randomPages[site.Id] = () => Randomness.Random.NextDouble() < homePct ? root : other();
            }


            segment.VisitVariables.AddOrReplace(new LandingPageVariable(() =>
            {
                var page = parser.InfoClient.GetItemInfo(landingPage());
                var site = parser.InfoClient.Sites[siteId()];
                if (page == null || Randomness.Random.NextDouble() < randomPagePct)
                {
                    return Tuple.Create(site, randomPages[site.Id]());
                }

                for (var i = 0; i < 10 && !page.Path.StartsWith(site.StartPath); i++)
                {
                    site = parser.InfoClient.Sites[siteId()];
                }

                return Tuple.Create(site, page);
            }, parser.InfoClient));
        }

        public override void SetDefaults(VisitorSegment segment, XGenParser parser)
        {
            UpdateSegment(segment, new JObject(), parser);
        }

        IEnumerable<Tuple<ItemInfo, int>> GetDescendants(IEnumerable<ItemInfo> items, int depth = 1)
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

        class LandingPageVariable : VisitorVariablesBase
        {
            private readonly Func<Tuple<SiteInfo, ItemInfo>> _siteAndItem;
            private readonly ItemInfoClient _itemInfo;


            public LandingPageVariable(Func<Tuple<SiteInfo, ItemInfo>> siteAndItem, ItemInfoClient itemInfo)
            {
                _siteAndItem = siteAndItem;
                _itemInfo = itemInfo;

                DependentVariables.Add("Language"); //TODO: Language?
                DependentVariables.Add("Campaign");
            }

            public override void SetValues(SimulationObject target)
            {
                var data = _siteAndItem();
                var site = data.Item1;
                var item = data.Item2;

                target.Variables.Add("Site", site.Id);
                if (!string.IsNullOrEmpty(site.Host))
                {
                    target.Variables.Add("Host", "http://" + site.Host.Split('|')[0]);
                }

                var lang = target.GetVariable<string>("Language");
                if (!string.IsNullOrEmpty(lang) && item.Language != lang)
                {
                    item = _itemInfo.GetItemInfo(item.Id.ToString(), lang);
                }

                var url = item.SiteUrls[site.Id].Url;
                if (url.Contains("://"))
                {
                    url = new Uri(url).PathAndQuery;
                }

                var campaignId = target.GetVariable<string>("Campaign");
                if (!string.IsNullOrEmpty(campaignId))
                {
                    url += (url.Contains("?") ? "&" : "?") + "sc_camp=" + campaignId;
                }

                target.Variables.Add("LandingPage", url);
            }

            public override IEnumerable<string> ProvidedVariables
            {
                get { return new[] { "Site", "Host", "LandingPage" }; }
            }

        }

    }
}
