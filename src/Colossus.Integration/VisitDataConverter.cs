using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Integration.Processing;
using FiftyOne.Foundation.Mobile.Detection;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;

namespace Colossus.Integration
{
    public static class VisitDataConverter
    {

        public static IEnumerable<VisitData> ToVisitData(this Visitor visitor)
        {

            var matcher = WebProvider.ActiveProvider;

            var contactId = Guid.NewGuid();

            var index = 0;
            foreach (var visit in visitor.Visits)
            {
                var vd = new VisitData(contactId);
                vd.ContactId = contactId;
                vd.ContactVisitIndex = index++;
                vd.StartDateTime = visit.Start;
                vd.EndDateTime = visit.End;

                vd.ChannelId = visit.GetVariable("Channel", Guid.Empty);

                vd.Value = (int) Math.Round(visit.GetVariable("Value", 0d));

                var referrer = visit.GetVariable<string>("Referrer");
                if (!string.IsNullOrEmpty(referrer))
                {
                    if (!referrer.StartsWith("http://"))
                    {
                        referrer = "http://" + referrer;
                    }
                    vd.Referrer = referrer;
                    vd.ReferringSite = new Uri(referrer).Host;
                }

                vd.Language = visit.GetVariable("Language", "en");

                var userAgent = visit.GetVariable<string>("UserAgent");
                if (!string.IsNullOrEmpty(userAgent))
                {
                    var m = matcher.Match(userAgent);
                    vd.UserAgent = userAgent;
                    vd.Browser = new BrowserData(m["BrowserVersion"].ToString(), m["BrowserName"].ToString(), m["BrowserVersion"].ToString());
                    vd.OperatingSystem = new OperatingSystemData(m["PlatformName"].ToString(), m["PlatformVersion"].ToString(), "");
                    vd.Screen = new ScreenData((int) m["ScreenPixelsWidth"].ToDouble(),
                        (int) m["ScreenPixelsHeight"].ToDouble());
                }
                else
                {
                    vd.Browser = new BrowserData("", "", "");
                    vd.OperatingSystem = new OperatingSystemData("", "", "");
                    vd.Screen = new ScreenData(0, 0);
                }

                vd.GeoData = new WhoIsInformation()
                {
                    Country = visit.GetVariable("Country", ""),
                    Region = visit.GetVariable("Region", ""),
                    City = visit.GetVariable("City", ""),
                    Latitude = visit.GetVariable<double?>("Latitude"),
                    Longitude = visit.GetVariable<double?>("Longitude")
                };

                vd.Pages = new List<PageData>();
                var pageIndex = 0;
                foreach (var req in visit.Requests)
                {
                    ++vd.VisitPageCount;
                    var page = new PageData();                    
                    page.VisitPageIndex = pageIndex++;
                    page.DateTime = req.Start;
                    
                    
                    var uri = new Uri(new Uri(req.GetVariable<string>("BaseUrl")), req.Url);
                    page.Url = new UrlData {Path = uri.AbsolutePath, QueryString = uri.Query};
                    page.Duration = (int) (req.End - req.Start).TotalMilliseconds;

                    page.Item = req.GetVariable("Item",
                        new ItemData {Id = Guid.Empty, Language = "en", Version = 1});

                    page.PageEvents = new List<PageEventData>();
                    var triggerEvents = req.GetVariable<IEnumerable<TriggerEventData>>("TriggerEvents");
                    if (triggerEvents != null)
                    {
                        foreach (var te in triggerEvents)
                        {
                            var pe = new PageEventData
                            {
                                ItemId = page.Item.Id,
                                PageEventDefinitionId = te.Id ?? Guid.Empty,
                                Name = te.Name,
                                Text = te.Text,
                                Value = te.Value ?? 0,
                                IsGoal = te.IsGoal,
                                DateTime = page.DateTime                                
                            };
                            
                            vd.Value += pe.Value;

                            if (te.CustomValues != null)
                            {
                                foreach (var cv in te.CustomValues)
                                {
                                    pe.CustomValues.Add(cv.Key, cv.Value);
                                }
                            }

                            page.PageEvents.Add(pe);
                        }
                    }

                    vd.Pages.Add(page);
                    
                }
                
                yield return vd;
            }

        }

    }
}
