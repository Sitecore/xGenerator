using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;

namespace Colossus.Web
{
    public class WebVisitRequestContext<TResponseInfo> : IVisitRequestContext<TResponseInfo>        
        where TResponseInfo : ResponseInfo, new()
    {

        public Request LastRequest
        {
            get { return Visit.Requests.LastOrDefault(); }
        }

        public TResponseInfo LastResponse
        {
            get { return VisitorContext.LastResponse; }
        }
       
        public event EventHandler<VisitEventArgs> VisitEnded;
        
        public WebRequestContext<TResponseInfo> VisitorContext { get; private set; }

        public Visit Visit { get; private set; }        

        internal protected WebVisitRequestContext(WebRequestContext<TResponseInfo> visitorContext, Visit visit)
        {
            Visit = visit;
            VisitorContext = visitorContext;
        }

        private TimeSpan _pause;
        public void Pause(TimeSpan duration)
        {
            _pause += duration;
        }

        protected TimeSpan GetAndResetPause()
        {
            var pause = _pause;
            _pause = TimeSpan.Zero;
            return pause;
        }

        protected virtual TResponseInfo Execute(Request request)
        {
            return VisitorContext.Execute(request);
        }

        public TResponseInfo Request(string url, TimeSpan? duration = null, object variables = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Visit has ended");
            }            

            var request = Visit.AddRequest(VisitorContext.TransformUrl(url, Visit), duration, GetAndResetPause());            
            if (variables != null)
            {
                foreach (var kv in (variables as IDictionary<string, object>) ?? new RouteValueDictionary(variables))
                {
                    request.Variables.Add(kv.Key, kv.Value);
                }
            }

            return Execute(request);
        }

        private bool _disposed;

        protected virtual void EndVisit()
        {            
            OnVisitEnded(new VisitEventArgs(Visit));
        }

        protected virtual void OnVisitEnded(VisitEventArgs e)
        {
            EventHandler<VisitEventArgs> handler = VisitEnded;
            if (handler != null) handler(this, e);
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                EndVisit();
                _disposed = true;
            }
            else
            {
                throw new ObjectDisposedException("Visit already disposed");
            }
        }
    }
}
