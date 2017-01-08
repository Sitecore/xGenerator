using System.Web;
using System.Web.SessionState;

namespace Colossus.Integration
{
    public class ColossusHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            var info = context.ColossusInfo();
            if (info != null && info.EndVisit)
            {
                context.Session.Abandon();
            }
        }

        public bool IsReusable => false;
    }
}
