using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web.Hosting;
using Colossus.Web;
using Newtonsoft.Json;
using Sitecore.ApplicationCenter.Applications;

namespace Colossus
{
    public class RequestLogRecord
    {
        public RequestInfo Info { get; set; }
        public string Url { get; set; }
        public string UserAgent { get; set; }
        public string Referer { get; set; }
    }

    public abstract class WebRequestContext<TResponseInfo> : IRequestContext<TResponseInfo>
        where TResponseInfo : ResponseInfo, new()
    {
        public Visitor Visitor { get; set; }

        public string BaseUrl { get; set; }


        protected WebClient WebClient { get; private set; }


        public event EventHandler<VisitEventArgs> VisitStarted;        

        public event EventHandler<VisitEventArgs> VisitEnded;

        public event EventHandler<RequestEventArgs> RequestStarted;

        public event EventHandler<RequestEventArgs> RequestEnded;       


        public bool ThrowWebExceptions { get; set; }

        public WebRequestContext(Visitor visitor)
        {
            Visitor = visitor;
            WebClient = new CookieCollectingWebClient(this);
            ThrowWebExceptions = true;
        }

        protected Request CurrentRequest { get; private set; }

        public TResponseInfo LastResponse { get; private set; }

        private TimeSpan _pause = TimeSpan.Zero;

        public void Pause(TimeSpan duration)
        {
            _pause += duration;
        }

        public IVisitRequestContext<TResponseInfo> NewVisit()
        {
            var ctx = CreateVisitContext(Visitor.AddVisit(GetAndResetPause()));
            OnVisitStarted(new VisitEventArgs(ctx.Visit));
            ctx.VisitEnded += (sender, args) => OnVisitEnded(args);

            return ctx;
        }

        protected abstract IVisitRequestContext<TResponseInfo> CreateVisitContext(Visit visit);

        protected TimeSpan GetAndResetPause()
        {
            var pause = _pause;
            _pause = TimeSpan.Zero;
            return pause;
        }


        public virtual string TransformUrl(string uri, Visit visit = null)
        {
            return uri;
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

        private static void SaveRequestToStorage(RequestInfo info, string url, string userAgent, string referer)
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

            using (StreamWriter sw = File.AppendText(@"G:\sitecore-xerox\src\ExperienceGenerator\urlsTime.log"))
            {
                sw.WriteLine(json);
            }
        }

        internal TResponseInfo Execute(Request request, Func<string, WebClient, string> requestAction = null)
        {
            OnRequestStarted(new RequestEventArgs(request));

            LastResponse = null;
            CurrentRequest = request;

            requestAction = requestAction ?? ((url, wc) => VisitUrl(wc,url));

            var response = requestAction(request.Url, WebClient);
            if (LastResponse != null)
            {
                LastResponse.Response = response;
            }

            OnRequestEnded(new RequestEventArgs(request));

            return LastResponse;
        }        
       

        public virtual void PrepareRequest(WebRequest request)
        {
            var info = RequestInfo.FromVisit(CurrentRequest);            
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.Timeout = 500000;
                httpRequest.UserAgent = CurrentRequest.GetVariable("UserAgent", "Colossus");
                httpRequest.Referer = CurrentRequest.GetVariable("Referrer", CurrentRequest.GetVariable("Referer", ""));
            }
            SaveRequestToStorage(info, httpRequest.RequestUri.ToString(), httpRequest.UserAgent, httpRequest.Referer);
            request.Headers.AddChunked(DataEncoding.RequestDataKey, DataEncoding.EncodeHeaderValue(info));
        }

        public virtual void ParseResponse(WebResponse response)
        {
            var data = response.Headers.GetChunked(DataEncoding.ResponseDataKey);
            if (data != null)
            {
                LastResponse = DataEncoding.DecodeHeaderValue<TResponseInfo>(data);
            }
            else
            {
                LastResponse = new TResponseInfo();
            }

            LastResponse.ParseHttpResponse(response as HttpWebResponse);
        }

        protected virtual void OnVisitStarted(VisitEventArgs e)
        {
            EventHandler<VisitEventArgs> handler = VisitStarted;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnVisitEnded(VisitEventArgs e)
        {
            EventHandler<VisitEventArgs> handler = VisitEnded;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnRequestStarted(RequestEventArgs e)
        {
            EventHandler<RequestEventArgs> handler = RequestStarted;
            if (handler != null) handler(this, e);
        }
        protected virtual void OnRequestEnded(RequestEventArgs e)
        {
            EventHandler<RequestEventArgs> handler = RequestEnded;
            if (handler != null) handler(this, e);
        }


        private class CookieCollectingWebClient : WebClient
        {
            private readonly WebRequestContext<TResponseInfo> _context;
            private readonly CookieContainer _container = new CookieContainer();

            public CookieCollectingWebClient(WebRequestContext<TResponseInfo> context)
            {
                _context = context;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                var webRequest = request as HttpWebRequest;
                if (webRequest != null)
                {
                    webRequest.Timeout = 500000;
                    webRequest.CookieContainer = _container;
                    webRequest.MaximumResponseHeadersLength = -1;
                }

                _context.PrepareRequest(request);

                return request;
            }



            private static object _writeLock = new object();
            protected override WebResponse GetWebResponse(WebRequest request)
            {
                WebResponse response;
                try
                {
                    response = base.GetWebResponse(request);
                }
                catch (WebException wex)
                {
                    if (_context.ThrowWebExceptions)
                    {
                        if (wex.Response != null)
                        {
                            lock (_writeLock)
                            {
                                var stream = wex.Response.GetResponseStream();
                                if (stream != null)
                                {
                                    var path = "LastException.html";
                                    if (HostingEnvironment.VirtualPathProvider != null)
                                    {
                                        path = HostingEnvironment.MapPath("~/temp/LastException.htm");
                                    }
                                    File.WriteAllText(path, new StreamReader(stream).ReadToEnd());
                                }
                            }
                        }
                        throw;
                    }

                    Console.Out.WriteLine("EXCEPTION: {0} ({1})", wex.Message, request.RequestUri);

                    response = wex.Response;
                }


                _context.ParseResponse(response);

                return response;
            }
        }
    }
}
