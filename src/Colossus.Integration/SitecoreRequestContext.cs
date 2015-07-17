using System;
using System.IO;
using System.Net;
using Colossus.Web;
using Newtonsoft.Json;

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
            var result = client.DownloadString(url);
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

        public override void PrepareRequest(WebRequest request)
        {
            var info = RequestInfo.FromVisit(CurrentRequest);
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.Timeout = 500000;
                httpRequest.UserAgent = CurrentRequest.GetVariable("UserAgent", "Colossus");
                httpRequest.Referer = CurrentRequest.GetVariable("Referrer", CurrentRequest.GetVariable("Referer", ""));
            }
            var outputRecordLogFile = GlobalParameters.Instance.OutputRecordLog;
            if (!string.IsNullOrEmpty(outputRecordLogFile))
            {
                SaveRequestToStorage(info, httpRequest.RequestUri.ToString(), httpRequest.UserAgent, httpRequest.Referer, outputRecordLogFile);
            }
            request.Headers.AddChunked(DataEncoding.RequestDataKey, DataEncoding.EncodeHeaderValue(info));            
        }

        private void SaveRequestToStorage(RequestInfo info, string url, string userAgent, string referer, string outputRecordLogFile)
        {
            var logRecord = new RequestLogRecord();
            logRecord.Info = info;
            logRecord.Url = url;
            logRecord.UserAgent = userAgent;
            logRecord.Referer = referer;

            var json = JsonConvert.SerializeObject(logRecord, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            using (StreamWriter sw = File.AppendText(outputRecordLogFile))
            {
                sw.WriteLine(json);
            }
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
