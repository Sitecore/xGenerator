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
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization;
using Sitecore.Globalization;

namespace Colossus.Integration.Processing
{
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.Definitions.Outcomes.Model;

  public class TriggerOutcomesAction : IRequestAction
    {        
        public void Execute(ITracker tracker, RequestInfo requestInfo)
        {
            var outcomes = requestInfo.Variables.GetOrDefault("TriggerOutcomes") as IEnumerable<TriggerOutcomeData>;
            if (outcomes != null)
            {
                var ix = 0;
                foreach (var o in outcomes)
                {
                    
                    var defintion = DefinitionManagerFactory.Default.GetDefinitionManager<IOutcomeDefinition>().Get(o.DefinitionId.ToID(), LanguageManager.DefaultLanguage.CultureInfo);
                    if (defintion == null)
                    {
                        throw new Exception("Outcome not found");
                    }
                    var oc = new ContactOutcome(Guid.NewGuid().ToID(), defintion.Id, tracker.Contact.ContactId.ToID());                    
                    if (defintion.IsMonetaryValueApplicable)
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
                    var added = tracker.GetContactOutcomes().First(outcome => outcome.Id == oc.Id);

                    //Patch date time, and add 1 ms each time to avoid collisions in xProfile
                    added.DateTime = (o.DateTime ?? tracker.CurrentPage.DateTime).AddMilliseconds(ix + 1);

                    ++ix;
                }
            }            
        }
    }
}
