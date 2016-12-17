namespace Colossus.Integration.Models
{
  using System;
  using System.Net;

  public class SitecoreRequestContext : WebRequestContext<SitecoreResponseInfo>
    {
        public Uri SitecoreRootUri { get; set; }        

        public string ColossusHandlerUrl
        {
            get { return this.TransformUrl("/colossus.ashx"); }
        }

        public SitecoreRequestContext(string sitecoreRootUri, Visitor visitor) : base(visitor)
        {
            this.SitecoreRootUri = new Uri(sitecoreRootUri);
        }

        public bool IsColossusClientAvailable
        {
            get
            {
                try
                {
                    new WebClient().DownloadString(this.ColossusHandlerUrl);
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
            var host = this.SitecoreRootUri;
            if (visit != null)
            {
                var visitHost = visit.GetVariable<string>(VariableKey.Host);
                if (!string.IsNullOrEmpty(visitHost))
                {
                    host = new Uri(visitHost);
                }
            }
            return new Uri(host, uri).ToString();
        }


        public new SitecoreVisitRequestContext NewVisit()
        {
            return base.NewVisit() as SitecoreVisitRequestContext;
        }
        
    }
}
