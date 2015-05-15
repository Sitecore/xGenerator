using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Colossus.Web
{
    public class ResponseInfo
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Response { get; set; }


        public void ParseHttpResponse(HttpWebResponse response)
        {
            if (response == null) return;
        
            var httpResponse = response;
            StatusCode = httpResponse.StatusCode;
        }

        private HtmlDocument _document;
        [JsonIgnore]
        public HtmlNode DocumentNode
        {
            get
            {
                
                if (_document == null)
                {
                    try
                    {
                        _document = new HtmlDocument();
                        _document.LoadHtml(Response);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return _document.DocumentNode;
            }
        }
    }
}
