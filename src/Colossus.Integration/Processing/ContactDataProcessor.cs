using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Web;
using Sitecore.Analytics;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Outcome.Extensions;
using Sitecore.Analytics.Tracking;

namespace Colossus.Integration.Processing
{
  public class ContactDataProcessor : ISessionPatcher
  {
    public void UpdateSession(Session session, RequestInfo requestInfo)
    {

      requestInfo.SetIfVariablePresent("ContactId", session.Identify);

      requestInfo.SetIfVariablePresent("ContactFirstName", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.FirstName = name;
      });

      requestInfo.SetIfVariablePresent("ContactLastName", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.Surname = name;

      });

      requestInfo.SetIfVariablePresent("ContactEmail", email =>
      {
        var emails = session.Contact.GetFacet<IContactEmailAddresses>("Emails");
        emails.Preferred = "Primary";
        var entry = emails.Entries.Create("Primary");
        entry.SmtpAddress = email;
      });
    }
  }
}
