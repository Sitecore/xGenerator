using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Tracking;
using Sitecore.Sites;

namespace Colossus.Integration.Processing
{
    public class TimePatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {         

            session.Interaction.StartDateTime = requestInfo.Visit.Start;
            session.Interaction.EndDateTime = requestInfo.Visit.End;

            var page = session.Interaction.CurrentPage;
            if (page != null)
            {
                page.DateTime = requestInfo.Start;
                page.Duration = (int)Math.Round((requestInfo.End - requestInfo.Start).TotalMilliseconds);
            }
        }
    }
}
