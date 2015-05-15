using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Colossus.Integration.Processing;
using Colossus.Web;
using Newtonsoft.Json;
using Sitecore;
using Sitecore.Analytics;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Tracking;
using Sitecore.Layouts;
using Sitecore.Pipelines.RenderLayout;

namespace Colossus.Integration
{
    public class GetContextInfo : RenderLayoutProcessor
    {
        public List<IRequestAction> RequestActions { get; set; }

        public GetContextInfo()
        {
            RequestActions = new List<IRequestAction>();

                        
            RequestActions.Add(new TriggerEventsAction());            
            RequestActions.Add(new TriggerOutcomesAction());
        }

        public override void Process(RenderLayoutArgs args)
        {
            var ctx = HttpContext.Current;
            if (ctx != null )
            {
                var requestInfo = HttpContext.Current.ColossusInfo();                
                if (Tracker.Current != null && requestInfo != null)
                {                    
                    foreach (var action in RequestActions)
                    {
                        action.Execute(Tracker.Current, requestInfo);
                    }
                }

                var info = SitecoreResponseInfo.FromContext();
                if (info != null)
                {             
                    ctx.Response.Headers.AddChunked(DataEncoding.ResponseDataKey, DataEncoding.EncodeHeaderValue(info));
                }
            }
        }
    }
}
