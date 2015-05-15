using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Colossus.Integration.Processing;
using Sitecore.Analytics.Tracking;
using Sitecore.Globalization;

namespace Colossus.Integration
{
    public class ColossusHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {            
            var info = context.ColossusInfo();
            if (info != null && info.EndVisit)
            {
                context.Session.Abandon();
            }
        }

        public bool IsReusable { get { return false; } }
    }
}
