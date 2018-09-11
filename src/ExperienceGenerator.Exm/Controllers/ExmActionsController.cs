using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using ExperienceGenerator.Repositories;
using Newtonsoft.Json.Linq;
using Sitecore.Data;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExmActionsController : ApiController
    {
        private readonly GeoDataRepository _geoDataRepository;

        public ExmActionsController()
        {
            _geoDataRepository = new GeoDataRepository();
        }

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
            return _geoDataRepository.Continents.Select(continent => new SelectionOptionGroup
                                                      {
                                                          Label = continent.Name,
                                                          Options = continent.SubContinents.Select(x => new SelectionOption
                                                                                                  {
                                                                                                      Id = x.Code,
                                                                                                      Label = x.Name,
                                                                                                      DefaultWeight = 50
                                                                                                  })
                                                      }).ToList();
        }
    }
}
