using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Colossus.Web;
using Sitecore.Analytics;
//using Sitecore.Analytics.Outcome;
//using Sitecore.Analytics.Outcome.Extensions;
//using Sitecore.Analytics.Outcome.Model;
using Sitecore.Data.Managers;

namespace Colossus.Integration.Processing
{
    using Sitecore.Marketing.Definitions;
    using Sitecore.Marketing.Definitions.Outcomes.Model;

    public class TriggerOutcomesAction : IRequestAction
    {
        //private readonly IServiceProvider _serviceProvider;

        //public TriggerOutcomesAction(IServiceProvider serviceProvider)
        //{
        //    _serviceProvider = serviceProvider;
        //}

        public void Execute(ITracker tracker, RequestInfo requestInfo)
        {
            var outcomes = requestInfo.Variables.GetOrDefault("TriggerOutcomes") as IEnumerable<TriggerOutcomeData>;
            if (outcomes != null)
            {
                var ix = 0;
                foreach (var o in outcomes)
                {

                    //var defintion = DefinitionManagerFactory.Default.GetDefinitionManager<IOutcomeDefinition>().Get(o.DefinitionId.ToID(), LanguageManager.DefaultLanguage.CultureInfo);
                    var _serviceProvider = DependencyResolver.Current.GetService<IServiceProvider>();
                    var definitionManager = new DefinitionManagerFactory(_serviceProvider);
                    var definition = definitionManager.GetDefinitionManager<IOutcomeDefinition>().Get(o.DefinitionId, LanguageManager.DefaultLanguage.CultureInfo);

                    if (definition == null)
                    {
                        throw new Exception("Outcome not found");
                    }

                    Tracker.Current.CurrentPage.RegisterOutcome(Tracker.MarketingDefinitions.Outcomes[definition.Id], "USD", definition.IsMonetaryValueApplicable ? o.MonetaryValue : 0.0m);

                    var added = Tracker.Current.CurrentPage.Outcomes.First(x => x.OutcomeDefinitionId == definition.Id);

                    var utcTimeStamp = (o.DateTime ?? tracker.CurrentPage.DateTime).AddMilliseconds(ix + 1);

                    added.Timestamp = DateTime.SpecifyKind(utcTimeStamp, DateTimeKind.Utc);

                    //var oc = new Outcome(Guid.NewGuid().ToID(), definition.Id.ToID(), tracker.Contact.ContactId.ToID());                    
                    //if (definition.IsMonetaryValueApplicable)
                    //{
                    //    oc.MonetaryValue = o.MonetaryValue;    
                    //}

                    //if (o.CustomValues != null)
                    //{
                    //    foreach (var kv in o.CustomValues)
                    //    {
                    //        oc.CustomValues[kv.Key] = kv.Value;
                    //    }
                    //}

                    //tracker.RegisterContactOutcome(oc);
                    //var added = Tracker.Current.Interaction.Outcomes.First(outcome => outcome.OutcomeDefinitionId == definition.Id);
                    //var added = tracker.GetContactOutcomes().First(outcome => outcome.Id == oc.Id);

                    //Patch date time, and add 1 ms each time to avoid collisions in xProfile
                    //added.DateTime = (o.DateTime ?? tracker.CurrentPage.DateTime).AddMilliseconds(ix + 1);

                    ++ix;
                }
            }
        }
    }
}
