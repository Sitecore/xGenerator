using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Web
{
    public class RecordedWebClient : WebClient
    {
        private readonly CookieContainer _container = new CookieContainer();
        public RequestLogRecord Context { get; set; }

        public RecordedWebClient()
        {
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

            PrepareRequest(request);

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
                //if (_context.ThrowWebExceptions)
                //{
                    //if (wex.Response != null)
                    //{
                    //    lock (_writeLock)
                    //    {
                    //        var stream = wex.Response.GetResponseStream();
                    //        if (stream != null)
                    //        {
                    //            var path = "LastException.html";
                    //            if (HostingEnvironment.VirtualPathProvider != null)
                    //            {
                    //                path = HostingEnvironment.MapPath("~/temp/LastException.htm");
                    //            }
                    //            File.WriteAllText(path, new StreamReader(stream).ReadToEnd());
                    //        }
                    //    }
                    //}
                    //throw;
                //}

                Console.Out.WriteLine("EXCEPTION: {0} ({1})", wex.Message, request.RequestUri);

                response = wex.Response;
            }


            //_context.ParseResponse(response);

            return response;
        }

        public virtual void PrepareRequest(WebRequest request)
        {

            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.Timeout = 500000;
                httpRequest.UserAgent = Context.UserAgent;
                httpRequest.Referer = Context.Referer;
            }

            request.Headers.AddChunked(DataEncoding.RequestDataKey, DataEncoding.EncodeHeaderValue(Context.Info));
        }

        public virtual void ParseResponse(WebResponse response)
        {
            var data = response.Headers.GetChunked(DataEncoding.ResponseDataKey);
            //if (data != null)
            //{
            //    LastResponse = DataEncoding.DecodeHeaderValue<TResponseInfo>(data);
            //}
            //else
            //{
            //    LastResponse = new TResponseInfo();
            //}

            //LastResponse.ParseHttpResponse(response as HttpWebResponse);
        }
    }
}
