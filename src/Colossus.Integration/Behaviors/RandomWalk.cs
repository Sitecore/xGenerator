using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Integration.Models;
using Colossus.Integration.Processing;
using HtmlAgilityPack;
using Sitecore.Analytics;

namespace Colossus.Integration.Behaviors
{
    public class RandomWalk : SitecoreBehavior
    {
        public RandomWalk(string sitecoreUrl) : base(sitecoreUrl)
        {
        }

        protected override IEnumerable<Visit> Commit(SitecoreRequestContext ctx)
        {
            var visits = ctx.Visitor.GetVariable<double>(VariableKey.VisitCount, 1);

            for (var i = 0; i < visits; i++)
            {
                double pause;
                using (var visitContext = ctx.NewVisit())
                {
                    var outcomes = visitContext.Visit.GetVariable<IEnumerable<TriggerOutcomeData>>(VariableKey.TriggerOutcomes);
                    visitContext.Visit.Variables.Remove(VariableKey.TriggerOutcomes);

                    pause = visitContext.Visit.GetVariable<double>(VariableKey.Pause);

                    var landingPage = visitContext.Visit.GetVariable<string>(VariableKey.LandingPage);
                    if (string.IsNullOrEmpty(landingPage))
                    {
                        throw new Exception("Expected LandingPage");
                    }

                    var history = new List<string>();
                    var response = visitContext.Request(landingPage);
                    history.Add(landingPage);

                    var pageViews = (int) visitContext.Visit.GetVariable<double>(VariableKey.PageViews) - 1;

                    if (visitContext.Visit.GetVariable(VariableKey.Bounce, false))
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
                        var variables = new Dictionary<VariableKey, object>();
                        if (j == pageViews - 1 && outcomes != null)
                        {
                            foreach (var oc in outcomes)
                            {
                                oc.DateTime = visitContext.Visit.End;
                            }
                            variables.Add(VariableKey.TriggerOutcomes, outcomes);
                        }

                        var events = new List<TriggerEventData>();
                        if (j == internalSearchIndex)
                        {
                            var internalKeywords = visitContext.Visit.GetVariable<string>(VariableKey.InternalSearch);
                            if (!string.IsNullOrEmpty(internalKeywords))
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
                            variables.Add(VariableKey.TriggerEvents, events);
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
            return GetRandomLocalLinkOnPage(response);
        }

        private static string GetRandomLocalLinkOnPage(SitecoreResponseInfo response)
        {
            var localLinks = GetLocalLinksOnPage(response);
            return localLinks?.Length > 0 ? localLinks[Randomness.Random.Next(0, localLinks.Length)] : null;
        }

        private static string[] GetLocalLinksOnPage(SitecoreResponseInfo response)
        {
            var links = GetLinksOnPage(response);
            var localLinks = links?.Select(l => l.GetAttributeValue("href", "")).Where(l => l.StartsWith("/") && !l.EndsWith(".aspx"));
            return localLinks?.Distinct().ToArray();
        }

        private static HtmlNodeCollection GetLinksOnPage(SitecoreResponseInfo response)
        {
            return response.DocumentNode.SelectNodes("//a[@href]");
        }
    }
}
