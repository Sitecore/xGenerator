using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
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
