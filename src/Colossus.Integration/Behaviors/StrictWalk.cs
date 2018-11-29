using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colossus.Integration.Models;
using Colossus.Integration.Processing;
using Newtonsoft.Json;

namespace Colossus.Integration.Behaviors
{

    public class StrictWalk : SitecoreBehavior
  {
    private readonly List<PageDefinition> pages;

    [JsonConstructor]
    public StrictWalk(string sitecoreUrl, IEnumerable<PageDefinition> pages) : base(sitecoreUrl)
    {
      this.pages = pages.ToList();
    }

    protected override IEnumerable<Visit> Commit(SitecoreRequestContext ctx)
    {
      using (var visitContext = ctx.NewVisit())
      {
        var outcomes = visitContext.Visit.GetVariable<IEnumerable<TriggerOutcomeData>>("TriggerOutcomes")?.ToArray();
        visitContext.Visit.Variables.Remove("TriggerOutcomes");

        UploadContactPicture(visitContext.Visit.Variables);

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

    private void UploadContactPicture(Dictionary<string, object> variables)
    {
      if (!variables.ContainsKey("ContactPicture")) return;
      var base64 = (string)variables["ContactPicture"];
      if (string.IsNullOrEmpty(base64)) return;
      var database = Sitecore.Context.ContentDatabase;
      Sitecore.Resources.Media.MediaCreatorOptions options = new Sitecore.Resources.Media.MediaCreatorOptions
      {
        Database = database,
        FileBased =false,
        Destination = "/sitecore/media library/Images/xgen/"+Math.Abs(base64.GetHashCode()),
       };

      var match = Regex.Match(base64, @"data:(?<type>.+?),(?<data>.+)", RegexOptions.Compiled);
      using (Stream stream = new MemoryStream(Convert.FromBase64String(match.Groups["data"].Value)))
      {
        var item = Sitecore.Resources.Media.MediaManager.Creator.CreateFromStream(stream, "/contacts/", options);
        variables["ContactPicture"] = item.ID;
      }
    }
  }
}
