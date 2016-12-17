using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Routing;

namespace Colossus.Web
{
    public class WebVisitRequestContext<TResponseInfo> : IVisitRequestContext<TResponseInfo> where TResponseInfo : ResponseInfo, new()
    {
        public Request LastRequest => Visit.Requests.LastOrDefault();

        public TResponseInfo LastResponse => VisitorContext.LastResponse;

        public event EventHandler<VisitEventArgs> VisitEnded;

        public WebRequestContext<TResponseInfo> VisitorContext { get; }

        public Visit Visit { get; }

        protected internal WebVisitRequestContext(WebRequestContext<TResponseInfo> visitorContext, Visit visit)
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

        protected virtual TResponseInfo Execute(Request request, Func<string, WebClient, string> requestAction = null)
        {
            return VisitorContext.Execute(request, requestAction);
        }

        public TResponseInfo Request(string url, TimeSpan? duration = null, IDictionary<VariableKey, object> variables = null, Func<string, WebClient, string> requestAction = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Visit has ended");
            }

            var request = Visit.AddRequest(VisitorContext.TransformUrl(url, Visit), duration, GetAndResetPause());
            if (variables != null)
            {
                foreach (var kv in variables)
                {
                    request.Variables.Add(kv.Key, kv.Value);
                }
            }

            return Execute(request, requestAction);
        }

        private bool _disposed;

        protected virtual void EndVisit()
        {
            OnVisitEnded(new VisitEventArgs(Visit));
        }

        protected virtual void OnVisitEnded(VisitEventArgs e)
        {
            var handler = VisitEnded;
            handler?.Invoke(this, e);
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
