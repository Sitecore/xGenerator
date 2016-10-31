using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sitecore.Analytics.Core;
using Sitecore.Analytics.Pipelines.CommitSession;
using Sitecore.Diagnostics;

namespace Colossus.Integration.Processing
{
    public class PatchExmDateTimes : CommitSessionProcessor
    {
        public override void Process(CommitSessionPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
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
                    if (data!=null && data.Contains("FakeDateTime"))
                    {
                        var dataObj = JObject.Parse(data);
                        var fakeDateTime = dataObj["FakeDateTime"].Value<DateTime>().ToUniversalTime();
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