using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using ExperienceGenerator.Data;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using Newtonsoft.Json.Linq;
using Sitecore.Data;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExmActionsController : ApiController
    {
        [HttpGet]
        public IHttpActionResult PresetQuery()
        {
            var repo = new SettingsRepository();
            return Json(new
                        {
                            query = repo.GetPresetsQuery()
                        });
        }

        [HttpPost]
        public IHttpActionResult SaveSettings([FromBody] Preset preset)
        {
            var repo = new SettingsRepository();
            repo.Save(preset.Name, preset.Spec);
            return Json(new
                        {
                            message = "ok"
                        });
        }

        [HttpGet]
        public JObject SettingsPreset(string id)
        {
            var repo = new SettingsRepository();
            return repo.GetSettingPreset(new ID(id));
        }

        [HttpGet]
        public List<SelectionOptionGroup> Locations()
        {
            return GeoRegion.Regions.Select(region => new SelectionOptionGroup
                                                      {
                                                          Label = region.Label,
                                                          Options = region.SubRegions.Select(x => new SelectionOption
                                                                                                  {
                                                                                                      Id = x.Id,
                                                                                                      Label = x.Label,
                                                                                                      DefaultWeight = 50
                                                                                                  })
                                                      }).ToList();
        }

        //[HttpPost]
        //public IHttpActionResult Flush()
        //{
        //   var driver = new MongoDbDriver(ConfigurationManager.ConnectionStrings["analytics"].ConnectionString);
        //   driver.ResetDatabase();

        //   var item = (Context.ContentDatabase ?? Context.Database).GetItem("/sitecore/media library/Images/xgen");
        //   item?.Delete();

        //   var sql = new SqlReportingStorageProvider("reporting");
        //   sql.DeleteAllReportingData();


        //   var index = ContentSearchManager.GetIndex(CustomerIntelligenceConfig.ContactSearch.SearchIndexName);
        //   index.Reset();
        //   index.Refresh();
        //   using (var ctx = index.CreateUpdateContext())
        //   {
        //     ctx.Optimize();
        //   }

        //   return Ok();

        // }
    }
}