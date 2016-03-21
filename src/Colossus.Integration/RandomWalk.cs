using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Integration.Processing;
using Sitecore.Analytics;

namespace Colossus.Integration
{
  public class RandomWalk : SitecoreBehavior
    {
        public RandomWalk(string sitecoreUrl)
            : base(sitecoreUrl)
        {
        }

        protected override IEnumerable<Visit> Commit(SitecoreRequestContext ctx)
        {            
            var visits = ctx.Visitor.GetVariable<double>("VisitCount", 1);


            for (var i = 0; i < visits; i++)
            {
                double pause = 0;                
                using (var visitContext = ctx.NewVisit())
                {
                    var outcomes = visitContext.Visit.GetVariable<IEnumerable<TriggerOutcomeData>>("TriggerOutcomes");
                    visitContext.Visit.Variables.Remove("TriggerOutcomes");                    

                    pause = visitContext.Visit.GetVariable<double>("Pause");

                    var landingPage = visitContext.Visit.GetVariable<string>("LandingPage");
                    if (string.IsNullOrEmpty(landingPage))
                    {
                        throw new Exception("Expected LandingPage");
                    }

                    var history = new List<string>();
                    var response = visitContext.Request(landingPage);
                    history.Add(landingPage);

                    var pageViews = (int) visitContext.Visit.GetVariable<double>("PageViews") - 1;

                    if (visitContext.Visit.GetVariable("Bounce", false))
                    {
                        pageViews = 1;
                    }


                    var internalSearchIndex = Randomness.Random.Next(0, pageViews);

                    for (var j = 0; j < pageViews; j++)
                    {
                        var nextUrl = GetNextUrl(response);
                        if (string.IsNullOrEmpty(nextUrl))
                        {
                            nextUrl = history.Count > 1 ? history[history.Count - 2] : history[0];
                        }
                        else
                        {
                            history.Add(nextUrl);
                        }


                        //Add outcomes to last visit
                        var variables = new Dictionary<string, object>();
                        if (j == pageViews - 1 && outcomes != null)
                        {
                            foreach (var oc in outcomes)
                            {
                                oc.DateTime = visitContext.Visit.End;
                            }
                            variables.Add("TriggerOutcomes", outcomes);
                        }

                        var events = new List<TriggerEventData>();
                        if (j == internalSearchIndex)
                        {
                            var internalKeywords = visitContext.Visit.GetVariable<string>("InternalSearch");
                            if( !string.IsNullOrEmpty(internalKeywords))
                            {
                                events.Add(new TriggerEventData
                                {
                                    Name = "Local search",
                                    Id = AnalyticsIds.SearchEvent.ToGuid(),                                   
                                    Text = internalKeywords
                                });
                            }
                        }


                        if (events.Count > 0)
                        {
                            variables.Add("TriggerEvents", events);
                        }

                        response = visitContext.Request(nextUrl, variables: variables);
                    }

                    yield return visitContext.Visit;
                }

                ctx.Pause(TimeSpan.FromDays(pause));
            }
        }

        protected virtual string GetNextUrl(SitecoreResponseInfo response)
        {
            var links = response.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                var localLinks =
                        links.Select(l => l.GetAttributeValue("href", "")).Where(l => l.StartsWith("/") && !l.EndsWith(".aspx")).ToArray();
                if (localLinks.Length > 0)
                {
                    return localLinks[Randomness.Random.Next(0, localLinks.Length)];
                }
            }

            return null;
        }
    }
}
