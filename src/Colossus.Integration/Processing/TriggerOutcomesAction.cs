using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Outcome;
using Sitecore.Analytics.Outcome.Extensions;
using Sitecore.Analytics.Outcome.Model;
using Sitecore.Analytics.Tracking;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Serialization;

namespace Colossus.Integration.Processing
{
    public class TriggerOutcomesAction : IRequestAction
    {        
        public void Execute(ITracker tracker, RequestInfo requestInfo)
        {
            var outcomes = requestInfo.Variables.GetOrDefault("TriggerOutcomes") as IEnumerable<TriggerOutcomeData>;
            if (outcomes != null)
            {
                foreach (var o in outcomes)
                {
                    var defintion = Database.GetDatabase("master").GetItem(o.DefinitionId.ToID());
                    if (defintion == null || defintion.Versions.Count == 0)
                    {
                        throw new Exception("Outcome not found");
                    }
                    var oc = new ContactOutcome(Guid.NewGuid().ToID(), defintion.ID, tracker.Contact.ContactId.ToID());
                    oc.DateTime = DateTime.Now;
                    if (defintion["Monetary Value Applicable"] == "1")
                    {
                        oc.MonetaryValue = o.MonetaryValue;    
                    }
                    
                    if (o.CustomValues != null)
                    {
                        foreach (var kv in o.CustomValues)
                        {
                            oc.CustomValues[kv.Key] = kv.Value;
                        }
                    }
                    tracker.RegisterContactOutcome(oc);
                }
            }            
        }
    }
}
