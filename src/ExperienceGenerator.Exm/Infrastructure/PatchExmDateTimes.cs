using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Analytics.Core;
using Sitecore.CES.GeoIp.Core.Model;
using Sitecore.Analytics.Pipelines.CommitSession;
using Sitecore.Diagnostics;

namespace ExperienceGenerator.Exm.Infrastructure
{
    public class PatchExmDateTimes : CommitSessionProcessor
    {
        public override void Process(CommitSessionPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, nameof(args));
            Assert.IsNotNull(args.Session, "args.Session");
            Assert.IsNotNull(args.Session.Interaction, "args.Session.Interaction");

            var interaction = args.Session.Interaction;

            if (interaction.Pages == null || !interaction.Pages.Any())
            {
                return;
            }

            foreach (var page in interaction.Pages)
            {
                if (!IsExmPage(page))
                {
                    return;
                }

                foreach (var pageEvent in page.PageEvents)
                {
                    var data = pageEvent.Data;
                    var dataObj = JObject.Parse(data);

                    if (data.Contains("FakeDateTime"))
                    {
                        var fakeDateTime = dataObj["FakeDateTime"].Value<DateTime>().ToUniversalTime();
                        dataObj.Remove("FakeDateTime");
                        pageEvent.DateTime = fakeDateTime;
                        page.DateTime = fakeDateTime;
                        interaction.StartDateTime = fakeDateTime;
                        interaction.EndDateTime = fakeDateTime.AddMinutes(1);
                        interaction.SaveDateTime = fakeDateTime.AddMinutes(1);
                    }
                    else
                    {
                        pageEvent.DateTime = interaction.StartDateTime;
                        page.DateTime = interaction.StartDateTime;
                    }

                    if (data.Contains("GeoData"))
                    {
                        var geoData = dataObj["GeoData"].ToObject<WhoIsInformation>();
                        dataObj.Remove("GeoData");
                        interaction.SetWhoIsInformation(geoData);
                    }

                    pageEvent.Data = JsonConvert.SerializeObject(dataObj);
                }
            }
        }

        private bool IsExmPage(Page page)
        {
            if (!page.CustomValues.ContainsKey("ScExmHolder"))
            {
                return false;
            }

            if (page.PageEvents == null || !page.PageEvents.Any())
            {
                return false;
            }

            return true;
        }
    }
}
