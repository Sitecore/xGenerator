using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using System.Xml;
using Colossus.Integration;
using Sitecore;
using Sitecore.Analytics.Aggregation;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Cintel.Configuration;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Sites;
using Sitecore.Web;
using ExperienceGenerator.Client.Models;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;
using ExperienceGenerator.Parsing;
using SiteInfo = ExperienceGenerator.Models.SiteInfo;

namespace ExperienceGenerator.Client.Controllers
{
    public class ExperienceGeneratorActionsController : ApiController
    {


        static SiteInfo[] GetSites(bool all)
        {

            var excludedSites = new HashSet<string>();

            var exportNode = Factory.GetConfigNode("experienceGenerator/excludeSites") as XmlElement;
            if (exportNode != null)
            {
                var sites = exportNode.SelectNodes("site");
                if (sites != null)
                {
                    foreach (var site in sites.OfType<XmlElement>())
                    {
                        excludedSites.Add(site.GetAttribute("name"));
                    }
                }
            }


            return SiteManager.GetSites().Select(s => new SiteContext(new Sitecore.Web.SiteInfo(s.Properties))).Select(site => new SiteInfo
            {
                Id = site.Name,
                Host = site.HostName.Split('|')[0],
                StartPath = site.RootPath + site.StartItem,
                Label = site.Name,
                Database = site.Database != null ? site.Database.Name : ""
            }).Where(site => 
                all || !excludedSites.Contains(site.Id))
            .ToArray();
        }


        [HttpGet]
        public IEnumerable<SiteInfo> Websites(bool all = false)
        {
            return GetSites(all);
        }

        [HttpGet]
        public IEnumerable<ItemInfo> Items(string query, int? maxDepth = null)
        {
            var db = Database.GetDatabase("web");

            foreach (var item in db.SelectItems(query))
            {
                yield return ItemInfo.FromItem(item, GetSites(false).Select(w => w.Id), maxDepth);
            }
        }


        [HttpGet]
        public ConfigurationOptions Options()
        {
            var options = new ConfigurationOptions();

            options.Websites = GetSites(false).Select(s => new SelectionOption
            {
                Id = s.Id, Label = s.Label,
                DefaultWeight = s.Id == "website" ? 100 : 50
            }).ToList();
            options.Location = GeoArea.Areas.Select(area => new SelectionOption { Id = area.Id, Label = area.Label }).ToList();

            var db = Database.GetDatabase("master");
            var online = db.GetItem("{D07286FA-67CE-4D66-8783-0140B8B91EF1}");


            options.Version = "0.1";

            options.ChannelGroups =
                online.Children.Where(c => c.TemplateName == "Channel Group").Select(cg => new ChannelGroup
                {
                    Label = cg.Name,
                    Channels =
                        cg.Children.Where(c => c.TemplateName == "Channel")
                            .Select(c => new SelectionOption
                            {
                                Id = c.ID.ToString(), Label = c.Name,
                                DefaultWeight = c.Name == "Direct" ? 50 : 0
                            })
                            .OrderBy(item => item.Label)
                            .ToList()
                }).ToList();

            var outcomes = db.GetItem("{062A1E69-0BF6-4D6D-AC4F-C11D0F7DC1E1}");
            options.OutcomeGroups =
                outcomes.Children.Where(c => c.TemplateName == "Outcome Type").Select(cg => new OutcomeGroup()
                {
                    Label = cg.Name,
                    Channels =
                        cg.Children.Where(c => c.TemplateName == "Outcome Definition")
                            .Select(c => new SelectionOption { Id = c.ID.ToString(), Label = c.Name, DefaultWeight = 5})
                            .OrderBy(item => item.Label)
                            .ToList()
                }).ToList();

            var campaigns = db.GetItem("{EC095310-746F-4C1B-A73F-941863564DC2}");
            options.Campaigns = campaigns.Axes.GetDescendants().Where(item => item.TemplateName == "Campaign")
                .Select(item => new SelectionOption { Id = item.ID.ToString(), Label = item.Paths.FullPath.Substring(campaigns.Paths.FullPath.Length + 1), DefaultWeight = 10})
                .OrderBy(e => e.Label)
                .ToList();

            options.OrganicSearch =
                SearchEngine.SearchEngines.Where(e => !e.Ppc)
                    .Select(e => new SelectionOption { Id = e.Id, Label = e.Label })
                    .ToList();

            options.PpcSearch =
                SearchEngine.SearchEngines.Where(e => e.Ppc)
                    .Select(e => new SelectionOption { Id = e.Id, Label = e.Label })
                    .ToList();


            return options;
        }


        [HttpPost]
        public IHttpActionResult StopAll()
        {
            foreach (
                var job in XGenJobManager.Instance.Jobs.Where(job => job.JobStatus <= JobStatus.Paused))
            {
                job.Stop();
            }

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Flush()
        {
            var driver = new MongoDbDriver(ConfigurationManager.ConnectionStrings["analytics"].ConnectionString);
            driver.ResetDatabase();

            var sql = new SqlReportingStorageProvider("reporting");
            sql.ExcludedTableFromDataDeletion("dbo", "Segments");
            sql.DeleteAllReportingData();


            var index = ContentSearchManager.GetIndex(CustomerIntelligenceConfig.ContactSearch.SearchIndexName);            
            index.Reset();            
            index.Refresh();
            using (var ctx = index.CreateUpdateContext())
            {
                ctx.Optimize();
            }

            return Ok();
        }
    }
}