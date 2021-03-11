using System;
using System.IO;
using System.Net;
using System.Web.Hosting;
using Colossus.Web;
using Sitecore.ApplicationCenter.Applications;

namespace Colossus
{
    public abstract class WebRequestContext<TResponseInfo> : IRequestContext<TResponseInfo>
        where TResponseInfo : ResponseInfo, new()
    {
        public Visitor Visitor { get; set; }

        public string BaseUrl { get; set; }

        protected WebClient WebClient { get; private set; }

        public event EventHandler<VisitEventArgs> VisitStarted;        

        public event EventHandler<VisitEventArgs> VisitEnded;

        public bool ThrowWebExceptions { get; set; }

        protected WebRequestContext(Visitor visitor)
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

        internal TResponseInfo Execute(Request request)
        {
            WebRequest.DefaultWebProxy = null;

            LastResponse = null;
            CurrentRequest = request;
            var webClient = new WebClient { Proxy = null };
            var response = WebClient.DownloadString(request.Url);
            if (LastResponse != null)
            {
                LastResponse.Response = response;
            }

            return LastResponse;
        }        
       
        public virtual void PrepareRequest(WebRequest request)
        {
            var info = RequestInfo.FromVisit(CurrentRequest);            
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {                
                httpRequest.UserAgent = CurrentRequest.GetVariable("UserAgent", "Colossus");
                httpRequest.Referer = CurrentRequest.GetVariable("Referrer", CurrentRequest.GetVariable("Referer", ""));
            }
            request.Headers.AddChunked(DataEncoding.RequestDataKey, DataEncoding.EncodeHeaderValue(info));
            request.Headers.Add("X-DisableDemo", "true");
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

                    // #5950 Allow secure cookies when running in Docker
                    if (response?.Headers["Set-Cookie"] != null && request.RequestUri.Scheme != "https")
                    {
                        var container = new CookieContainer();
                        container.SetCookies(new Uri("https://localhost"), response?.Headers["Set-Cookie"]);
                        var cookies = container.GetCookies(new Uri("https://localhost"));

                        foreach (Cookie cookie in cookies)
                        {
                            if (cookie.Secure)
                            {
                                _container.Add(new Cookie(cookie.Name, cookie.Value, "/", request.RequestUri.Host));
                            }
                        }
                    }
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
