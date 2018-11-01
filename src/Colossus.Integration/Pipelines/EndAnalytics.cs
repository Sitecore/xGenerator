using Sitecore.Analytics;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using System;
using System.Web;

namespace Colossus.Integration
{
    public class EndAnalytics
    {
        public void Process(PipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");

            if (Tracker.Current == null)
            {
                return;
            }

            try
            {
                var requestInfo = HttpContext.Current.ColossusInfo();
                if (requestInfo == null)
                {
                    return;
                }

                var startDate = DateTime.SpecifyKind(requestInfo.Visit.Start, DateTimeKind.Utc);
                var endDate = DateTime.SpecifyKind(requestInfo.Visit.End, DateTimeKind.Utc);

                var pageEvents = Tracker.Current.Interaction.CurrentPage.PageEvents;

                foreach (var evnt in pageEvents)
                {
                    evnt.DateTime = endDate;
                    evnt.Timestamp = endDate.Ticks;
                }
            }
            catch (Exception ex)
            {
                //Log but ignore errors to avoid interrupting standard analytics
                Log.Error("xGenerator Colossus.Integration.EndAnalytics failed.", ex, this);
            }
        }
    }
}
