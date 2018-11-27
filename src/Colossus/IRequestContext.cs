using System;
using Colossus.Web;

namespace Colossus
{
    public interface IRequestContext<out TResponseInfo>
            where TResponseInfo : ResponseInfo            
    {
        Visitor Visitor { get; }        

        void Pause(TimeSpan duration);

        IVisitRequestContext<TResponseInfo> NewVisit();                 
    }
}
