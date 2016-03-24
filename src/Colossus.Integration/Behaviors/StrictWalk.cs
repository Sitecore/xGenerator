namespace Colossus.Integration.Behaviors
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Colossus.Integration.Models;
  using Colossus.Integration.Processing;

  public class StrictWalk : SitecoreBehavior
  {
    private readonly List<PageDefinition> pages;

    public StrictWalk(string sitecoreUrl, IEnumerable<PageDefinition> pages) : base(sitecoreUrl)
    {
      this.pages = pages.ToList();
    }

    protected override IEnumerable<Visit> Commit(SitecoreRequestContext ctx)
    {
      using (var visitContext = ctx.NewVisit())
      {
        var outcomes = visitContext.Visit.GetVariable<IEnumerable<TriggerOutcomeData>>("TriggerOutcomes");
        visitContext.Visit.Variables.Remove("TriggerOutcomes");
        for (var i = 0; i < this.pages.Count; i++)
        {
          var page = this.pages[i];

          if (outcomes != null && i == this.pages.Count - 2)
          {
            foreach (var oc in outcomes)
            {
              oc.DateTime = visitContext.Visit.End;
            }
            page.RequestVariables.Add("TriggerOutcomes", outcomes);
          }

          visitContext.Request(page.Path.Replace("/sitecore/media library/", "/-/media/"), TimeSpan.FromSeconds(Randomness.Random.Next(10, 200)), page.RequestVariables);
        }

        yield return visitContext.Visit;
      }
    }
  }
}