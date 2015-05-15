using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;

namespace Colossus
{
    public interface IVisitRequestContext<out TResponseInfo> : IDisposable
            where TResponseInfo : ResponseInfo
    {
        event EventHandler<VisitEventArgs> VisitEnded;

        Visit Visit { get; }

        void Pause(TimeSpan duration);

        TResponseInfo Request(string url, TimeSpan? duration = null, object variables = null, Func<string, WebClient, string> requestAction = null);
    }
}
