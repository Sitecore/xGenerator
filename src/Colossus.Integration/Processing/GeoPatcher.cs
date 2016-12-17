using Colossus.Web;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{
    public class GeoPatcher : ISessionPatcher
    {
        public void UpdateSession(Session session, RequestInfo requestInfo)
        {
            if (requestInfo.Visitor != null)
            {
                var whois = new WhoIsInformation();
                if (requestInfo.Visitor.Variables.SetIfPresent<VariableKey, string>(VariableKey.Country, v => whois.Country = v) | requestInfo.Visitor.Variables.SetIfPresent<VariableKey, string>(VariableKey.Region, v => whois.Region = v) | requestInfo.Visitor.Variables.SetIfPresent<VariableKey, string>(VariableKey.City, v => whois.City = v))
                {
                    session.Interaction.SetGeoData(whois);
                }
            }
        }
    }
}
