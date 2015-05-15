using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{    
    /// <summary>
    /// Classes implementing this interface are run while the tracker is initialized, and before rules based on the session are executed
    /// This is the right place to set geo data, device type, referrer etc.
    /// </summary>
    public interface ISessionPatcher
    {
        void UpdateSession(Session session, RequestInfo requestInfo);
    }
}
