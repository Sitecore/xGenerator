using System.Collections.Generic;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using Newtonsoft.Json.Linq;
using Sitecore.Data;

namespace ExperienceGenerator.Exm.Controllers
{
	using System.Web.Http;
	public class ExmActionsController : ApiController
	{
		[HttpGet]
		public IHttpActionResult PresetQuery()
		{
			var repo = new SettingsRepository();
			return this.Json(new { query = repo.GetPresetsQuery() });
		}

		[HttpPost]
		public IHttpActionResult SaveSettings([FromBody] Preset preset)
		{
			var repo = new SettingsRepository();
			repo.Save(preset.Name, preset.Spec);
			return this.Json(new { message = "ok" });
		}

		[HttpGet]
		public JObject SettingsPreset(string id)
		{
			var repo = new SettingsRepository();
			return repo.GetSettingPreset(new ID(id));
		}
	}
}