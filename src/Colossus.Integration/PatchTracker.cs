using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Colossus.Integration.Processing;
using Colossus.Web;
using Sitecore;
using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.InitializeTracker;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.ItemWebApi;

namespace Colossus.Integration
{
    public class PatchTracker : InitializeTrackerProcessor
    {
        public List<ISessionPatcher> Patchers { get; set; }

        public PatchTracker()
        {
            Patchers = new List<ISessionPatcher>();

            //TODO: Add to config
            Patchers.Add(new ChannelPatcher());            
            Patchers.Add(new GeoPatcher());
            Patchers.Add(new TimePatcher());
            Patchers.Add(new ContactDataProcessor());         
                        
        }

        public void AddPatcher(ISessionPatcher patcher)
        {
            Patchers.Add(patcher);
        }

        public override void Process(InitializeTrackerArgs args)
        {
            var requestInfo = HttpContext.Current.ColossusInfo();
            if (requestInfo != null)
            {                
                if (Tracker.Current != null && Tracker.IsActive)
                {
                    foreach (var patcher in Patchers)
                    {                           
                        patcher.UpdateSession(args.Session, requestInfo);
                    }
                }
            }
        }
    }
}
