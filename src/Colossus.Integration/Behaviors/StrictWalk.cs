using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colossus.Integration.Models;
using Colossus.Integration.Processing;
using Sitecore;
using Sitecore.Resources.Media;
using Convert = System.Convert;

namespace Colossus.Integration.Behaviors
{
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
                var outcomes = visitContext.Visit.GetVariable<IEnumerable<TriggerOutcomeData>>(VariableKey.TriggerOutcomes)?.ToArray();
                visitContext.Visit.Variables.Remove(VariableKey.TriggerOutcomes);

                UploadContactPicture(visitContext.Visit.Variables);

                for (var i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];

                    if (outcomes != null && i == pages.Count - 2)
                    {
                        foreach (var oc in outcomes)
                        {
                            oc.DateTime = visitContext.Visit.End;
                        }
                        page.RequestVariables.Add(VariableKey.TriggerOutcomes, outcomes);
                    }

                    visitContext.Request(page.Path.Replace("/sitecore/media library/", "/-/media/"), TimeSpan.FromSeconds(Randomness.Random.Next(10, 200)), page.RequestVariables);
                }

                yield return visitContext.Visit;
            }
        }

        private void UploadContactPicture(Dictionary<VariableKey, object> variables)
        {
            if (!variables.ContainsKey(VariableKey.ContactPicture))
                return;
            var base64 = (string) variables[VariableKey.ContactPicture];
            if (string.IsNullOrEmpty(base64))
                return;
            var database = Context.ContentDatabase;
            var options = new MediaCreatorOptions
                          {
                              Database = database,
                              FileBased = false,
                              Destination = "/sitecore/media library/Images/xgen/" + Math.Abs(base64.GetHashCode())
                          };

            var match = Regex.Match(base64, @"data:(?<type>.+?),(?<data>.+)", RegexOptions.Compiled);
            using (Stream stream = new MemoryStream(Convert.FromBase64String(match.Groups["data"].Value)))
            {
                var item = MediaManager.Creator.CreateFromStream(stream, "/contacts/", options);
                variables[VariableKey.ContactPicture] = item.ID;
            }

            // Sitecore.Data.Items.MediaItem mediaItem = new Sitecore.Data.Items.MediaItem(item);
        }
    }
}
