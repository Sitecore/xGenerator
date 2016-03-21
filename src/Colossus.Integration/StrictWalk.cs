namespace Colossus.Integration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Colossus.Integration.Processing;
  using Sitecore.Analytics;

  public class StrictWalk : SitecoreBehavior
  {
    private IEnumerable<string> pages;

   
    public StrictWalk(string sitecoreUrl, IEnumerable<string> pages) : base(sitecoreUrl)
    {
      this.pages = pages;
    }

    protected override IEnumerable<Visit> Commit(SitecoreRequestContext ctx)
    {

      double pause = 0;
      using (var visitContext = ctx.NewVisit())
      {

        foreach (var page in pages)
        {
          
          visitContext.Request(page, TimeSpan.FromSeconds(Randomness.Random.Next(10, 200)));
        }

        yield return visitContext.Visit;
      }

      ctx.Pause(TimeSpan.FromDays(pause));
    }
  }
}