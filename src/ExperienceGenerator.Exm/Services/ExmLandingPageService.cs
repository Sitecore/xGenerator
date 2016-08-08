namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Colossus.Statistics;
  using Sitecore.Common;
  using Sitecore.Data;
  using Sitecore.Links;

  public class ExmLandingPageService
  {
    private readonly Func<string> landingPages;

    public ExmLandingPageService(Dictionary<Guid, int> landingPages)
    {
      var database = Database.GetDatabase("web") ?? Database.GetDatabase("master");


      if (landingPages.Count < 1)
      {
        this.landingPages = () => "/";
      }
      else
      {
        this.landingPages = landingPages.ToDictionary(k => LinkManager.GetItemUrl(database.GetItem(k.Key.ToID())), v => v.Value).Weighted();
      }
    }


    public string GetLandingPage()
    {
      return this.landingPages();
     
    }
  }
}