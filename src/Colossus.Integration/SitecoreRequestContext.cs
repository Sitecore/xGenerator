using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private static string VisitUrl(WebClient client, string url)
        {
            //var stopWatch = new Stopwatch();
            //stopWatch.Start();
            var result = client.DownloadString(url);
            //stopWatch.Stop();
            //using (StreamWriter sw = File.AppendText(@"G:\sitecore-xerox\src\ExperienceGenerator\urlsTime.log"))
            //{
            //    sw.WriteLine(url + " Time:" + stopWatch.ElapsedMilliseconds);
            //}
            return result;
        }

        public bool IsColossusClientAvailable
        {
            get
            {
                try
                {
                    VisitUrl(new WebClient(), ColossusHandlerUrl);
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
