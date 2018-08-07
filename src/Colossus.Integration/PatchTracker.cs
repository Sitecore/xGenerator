using System;
using System.Collections.Generic;
using System.Web;
using Colossus.Integration.Processing;
using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.InitializeTracker;
using Sitecore.Diagnostics;

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
            if (Tracker.Current == null || !Tracker.IsActive)
            {
                return;
            }

            try
            {
                var requestInfo = HttpContext.Current.ColossusInfo();
                if (requestInfo == null)
                {
                    if (PatchExmRequestTime(args)) return;
                }

                if (requestInfo == null)
                {
                    return;
                }

                foreach (var patcher in Patchers)
                {
                    patcher.UpdateSession(args.Session, requestInfo);
                }
            }
            catch (Exception ex)
            {
                //Log but ignore errors to avoid interrupting standard analytics
                Log.Error("xGenerator PatchTracker failed.", ex, this);
            }
        }

        private bool PatchExmRequestTime(InitializeTrackerArgs args)
        {
            var exmRequestTime = args.HttpContext.Request.Headers["X-Exm-RequestTime"];
            if (string.IsNullOrEmpty(exmRequestTime))
            {
                return false;
            }

            DateTime requestTime;
            if (!DateTime.TryParse(exmRequestTime, out requestTime))
            {
                return false;
            }

            args.Session.Interaction.StartDateTime = requestTime;
            args.Session.Interaction.EndDateTime = requestTime.AddSeconds(5);
            return true;
        }
    }
}
