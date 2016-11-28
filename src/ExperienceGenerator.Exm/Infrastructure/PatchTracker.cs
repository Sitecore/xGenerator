using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Colossus;
using ExperienceGenerator.Exm.Models;
using Newtonsoft.Json;
using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.InitializeTracker;

namespace ExperienceGenerator.Exm.Infrastructure
{
	public class PatchTracker : InitializeTrackerProcessor
	{
		public override void Process(InitializeTrackerArgs args)
		{
			if (Tracker.Current == null || !Tracker.IsActive)
			{
				return;
			}

			var requestInfo = HttpContext.Current.ColossusInfo();
			if (requestInfo == null)
			{
				PatchFakeExmData(args);
			}
		}

		private void PatchFakeExmData(InitializeTrackerArgs args)
		{
			var fakeDataJson = args.HttpContext.Request.Headers["X-Exm-FakeData"];
			if (string.IsNullOrEmpty(fakeDataJson))
			{
				return;
			}

			var fakeData = JsonConvert.DeserializeObject<RequestHeaderInfo>(fakeDataJson);

			if (!string.IsNullOrEmpty(fakeData.UserAgent))
			{
				args.Session.Interaction.UserAgent = fakeData.UserAgent;
			}

			if (fakeData.RequestTime.HasValue)
			{
				args.Session.Interaction.StartDateTime = fakeData.RequestTime.Value;
				args.Session.Interaction.EndDateTime = fakeData.RequestTime.Value.AddSeconds(5);
			}

			if (fakeData.GeoData != null)
			{
				args.Session.Interaction.SetGeoData(fakeData.GeoData);
                args.Session.Interaction.UpdateLocationReference();

            }
		}
	}
}
