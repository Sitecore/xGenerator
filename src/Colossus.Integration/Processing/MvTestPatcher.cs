using System;
using System.Linq;
using Colossus.Web;
using Sitecore.Analytics.Tracking;
using Sitecore.ContentTesting.Model.Extensions;
using Sitecore.Data;

namespace Colossus.Integration.Processing
{
    public class MvTestPatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            string testId = requestInfo.Visit.Variables.GetOrDefault("MvTestId") as string;
            string combination = requestInfo.Visit.Variables.GetOrDefault("PreferredExperience") as string;

            DateTime interactionDateTime = session.Interaction.StartDateTime;
           
            Sitecore.Data.Items.Item testItem = (Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database).GetItem(testId);
            if(testItem != null)
            {
                DataUri contentItemUri = DataUri.Parse(testItem["Content Item"]);

                if(session.Interaction.CurrentPage.Item.Id == contentItemUri.ItemID.Guid)
                {
                    session.Interaction.CurrentPage.MvTest = new Sitecore.Analytics.Model.MvTestData()
                    {
                        Id = new Guid(testId),
                        Combination = combination.ParseFromMultiplexedString("-").ToArray<byte>(),
                        ExposureTime = interactionDateTime,
                        FirstExposure = true
                    };
                }
            }
        }
    }
}
