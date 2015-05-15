using System;
using System.Net;
using Colossus.Web;
using Sitecore.Analytics.Model;

namespace Colossus.Integration
{
    public class SitecoreVisitRequestContext : WebVisitRequestContext<SitecoreResponseInfo>
    {
        public new SitecoreRequestContext VisitorContext
        {
            get { return base.VisitorContext as SitecoreRequestContext; }
        }

        public SitecoreVisitRequestContext(WebRequestContext<SitecoreResponseInfo> visitorContext, Visit visit)
            : base(visitorContext, visit)
        {

        }

        public VisitData VisitData { get; protected set; }

        protected override void EndVisit()
        {
            var req = new Request { Visit = Visit, Url = VisitorContext.ColossusHandlerUrl, EndVisit = true };
            Execute(req);
            base.EndVisit();
        }

        protected override SitecoreResponseInfo Execute(Request request, Func<string, WebClient, string> requestAction = null)
        {
            var response = base.Execute(request, requestAction);
            if (response.VisitData != null)
            {
                VisitData = response.VisitData;
            }
            return response;
        }
    }
}
