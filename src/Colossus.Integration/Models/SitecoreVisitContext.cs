namespace Colossus.Integration.Models
{
  using System;
  using System.Net;
  using Colossus.Web;
  using Sitecore.Analytics.Model;

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
            var req = new Request { Visit = this.Visit, Url = this.VisitorContext.ColossusHandlerUrl, EndVisit = true };
            this.Execute(req);
            base.EndVisit();
        }

        protected override SitecoreResponseInfo Execute(Request request)
        {
            var response = base.Execute(request);
            if (response.VisitData != null)
            {
                this.VisitData = response.VisitData;
            }
            return response;
        }
    }
}
