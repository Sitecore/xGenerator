using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Web;
using Sitecore;
using Sitecore.Analytics;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.ContentTesting.Extensions;
using Sitecore.Layouts;

namespace Colossus.Integration.Models
{
    public class SitecoreResponseInfo : ResponseInfo
    {
        public Guid? ContactId { get; set; }

        public VisitData VisitData { get; set; }

        public Dictionary<string, object> ResponseData { get; set; }

        public List<RenderingInfo> Renderings { get; set; }

        public ItemInfo Item { get; set; }


        public TestInfo Test { get; set; }

        public SitecoreResponseInfo()
        {
            Renderings = new List<RenderingInfo>();
        }

        public static SitecoreResponseInfo FromContext()
        {
            if (Tracker.Current == null)
                return null;

            var info = new SitecoreResponseInfo
                       {
                           ContactId = Tracker.Current.Contact.TryGetValue(c => (Guid?) c.ContactId),
                           VisitData = ((IUpdatableObject) Tracker.Current.Interaction).TryGetValue(i => i.GetParts().OfType<VisitData>().FirstOrDefault())
                       };


            if (Context.Page != null)
            {
                foreach (RenderingReference rendering in Context.Page.Renderings)
                {
                    var s = rendering.Settings;
                    var renderingInfo = new RenderingInfo
                                        {
                                            Item = rendering.RenderingItem.TryGetValue(item => ItemInfo.FromItem(item.InnerItem))
                                        };
                    if (s != null)
                    {
                        renderingInfo.Conditions = s.Conditions;
                        renderingInfo.DataSource = s.DataSource;
                        renderingInfo.MultiVariateTest = s.MultiVariateTest;
                        renderingInfo.Parameters = s.Parameters;
                        renderingInfo.PersonalizationTest = s.PersonalizationTest;
                        renderingInfo.Placeholder = s.Placeholder;
                    }

                    info.Renderings.Add(renderingInfo);
                }
            }

            try
            {
                info.Test = TestInfo.FromTestCombination(Tracker.Current.CurrentPage.GetTestCombination());
            }
            catch (Exception ex)
            {
                if (info.ResponseData != null)
                {
                    info.ResponseData["TestError"] = ex.ToString();
                }
            }

            if (Context.Item != null)
            {
                info.Item = ItemInfo.FromItem(Context.Item);
            }

            return info;
        }
    }
}
