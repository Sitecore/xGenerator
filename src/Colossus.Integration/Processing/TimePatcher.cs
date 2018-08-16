using System;
using Colossus.Web;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{
    public class TimePatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            var startDate = DateTime.SpecifyKind(requestInfo.Visit.Start, DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(requestInfo.Visit.End, DateTimeKind.Utc);

            session.Interaction.StartDateTime = startDate;
            session.Interaction.EndDateTime = endDate;

            var page = session.Interaction.CurrentPage;
            if (page != null)
            {
                var requestInfoStart = DateTime.SpecifyKind(requestInfo.Start, DateTimeKind.Utc);
                var requestInfoEnd = DateTime.SpecifyKind(requestInfo.End, DateTimeKind.Utc);

                page.DateTime = requestInfoStart;
                page.Duration = (int)Math.Round((requestInfoEnd - requestInfoStart).TotalMilliseconds);
            }
        }
    }
}
