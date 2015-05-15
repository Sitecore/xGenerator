using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Colossus.Integration.Processing;
using Colossus.Web;
using Sitecore.Analytics.Model;

namespace Colossus.Integration
{
    public class SitecoreRequestContext : WebRequestContext<SitecoreResponseInfo>
    {
        public Uri SitecoreRootUri { get; set; }        

        public string ColossusHandlerUrl
        {
            get { return TransformUrl("/sitecore/admin/colossus.ashx"); }
        }

        public SitecoreRequestContext(string sitecoreRootUri, Visitor visitor) : base(visitor)
        {
            SitecoreRootUri = new Uri(sitecoreRootUri);
        }

        public bool IsColossusClientAvailable
        {
            get
            {
                try
                {
                    new WebClient().DownloadString(ColossusHandlerUrl);
                    return true;
                }
                catch
                {
                }

                return false;
            }
        }

        protected override IVisitRequestContext<SitecoreResponseInfo> CreateVisitContext(Visit visit)
        {
            return new SitecoreVisitRequestContext(this, visit);
        }

        public override string TransformUrl(string uri, Visit visit = null)
        {
            var host = SitecoreRootUri;
            if (visit != null)
            {
                var visitHost = visit.GetVariable<string>("Host");
                if (visitHost != null)
                {
                    host = new Uri(visitHost);
                }
            }
            return new Uri(SitecoreRootUri, uri).ToString();
        }


        public new SitecoreVisitRequestContext NewVisit()
        {
            return base.NewVisit() as SitecoreVisitRequestContext;
        }
        
    }
}
