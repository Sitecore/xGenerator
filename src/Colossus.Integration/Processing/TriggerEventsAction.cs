using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Data;
//using Sitecore.Analytics.Outcome.Extensions;
using Sitecore.Analytics.Tracking;
//using Sitecore.ExperienceAnalytics.Aggregation.Dimensions;

namespace Colossus.Integration.Processing
{
    public class TriggerEventsAction : IRequestAction
    {
        public void Execute(ITracker tracker, RequestInfo requestInfo)
        {            
            var events = requestInfo.Variables.GetOrDefault("TriggerEvents") as IEnumerable<TriggerEventData>;

            if (events != null)
            {           
                foreach (var e in events)
                {                    
                    var eventData = tracker.Interaction.CurrentPage.Register(new PageEventData(e.Name, e.Id ?? Guid.Empty) {Text = e.Text});

                    if (e.CustomValues != null)
                    {
                        foreach (var kv in e.CustomValues)
                        {
                            eventData.CustomValues[kv.Key] = kv.Value;
                        }
                    }
                }
            }
        }
    }
}
