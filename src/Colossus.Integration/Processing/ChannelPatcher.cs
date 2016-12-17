using System;
using Colossus.Web;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{
    public class ChannelPatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            requestInfo.SetIfVariablePresent(VariableKey.Channel, value =>
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
