using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics.Tracking;
using Sitecore.Data;

namespace Colossus.Integration.Processing
{
    public class ChannelPatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            requestInfo.SetIfVariablePresent("Channel", (value) =>
            {
              if (string.IsNullOrEmpty(value))
              {
                return;
              }

              Guid id;
              if (Guid.TryParse(value, out id))
              {
                session.Interaction.ChannelId = id;
              }
            });            
        }
    }
}
