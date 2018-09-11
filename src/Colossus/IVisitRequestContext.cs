using System;
using Colossus.Web;

namespace Colossus
{
    public interface IVisitRequestContext<out TResponseInfo> : IDisposable
            where TResponseInfo : ResponseInfo
    {
        event EventHandler<VisitEventArgs> VisitEnded;

        Visit Visit { get; }

        void Pause(TimeSpan duration);

        TResponseInfo Request(string url, TimeSpan? duration = null, object variables = null);
    }
}
