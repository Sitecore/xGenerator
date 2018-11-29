using System;
using System.Web;
using Sitecore.Analytics;

namespace ExperienceGenerator.Exm.sitecore
{
    public partial class EndSession : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Tracker.Current != null)
                Tracker.Current.EndVisit(true);
            HttpContext.Current.Session.Abandon();
        }
    }
}
