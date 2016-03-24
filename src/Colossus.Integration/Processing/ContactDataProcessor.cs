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

      requestInfo.SetIfVariablePresent("ContactMiddleName", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.MiddleName = name;

      });

      requestInfo.SetIfVariablePresent("ContactBirthDate", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.BirthDate = DateTime.ParseExact(name.Substring(0, 8), "yyyyMMdd", null);

      });

      requestInfo.SetIfVariablePresent("ContactGender", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.Gender = name;

      });

      requestInfo.SetIfVariablePresent("ContactJobTitle", name =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.JobTitle = name;

      });

      requestInfo.SetIfVariablePresent("ContactEmail", email =>
      {
        var emails = session.Contact.GetFacet<IContactEmailAddresses>("Emails");
        var preferred = "Primary";
        emails.Preferred = preferred;
        var entry = emails.Entries.Contains(preferred)
          ? emails.Entries[preferred]
          : emails.Entries.Create(preferred);
        entry.SmtpAddress = email;
      });

      requestInfo.SetIfVariablePresent("ContactPhone", phoneNumber =>
      {
        var phoneNumbers = session.Contact.GetFacet<IContactPhoneNumbers>("Phone Numbers");
        var preferred = "Primary";
        phoneNumbers.Preferred = preferred;
        var entry = phoneNumbers.Entries.Contains(preferred)
          ? phoneNumbers.Entries[preferred]
          : phoneNumbers.Entries.Create(preferred);
        entry.Number = phoneNumber;
      });

      requestInfo.SetIfVariablePresent("ContactAddress", address =>
      {
        var phoneNumbers = session.Contact.GetFacet<IContactAddresses>("Addresses");
        var preferred = "Primary";
        phoneNumbers.Preferred = preferred;
        var entry = phoneNumbers.Entries.Contains(preferred)
          ? phoneNumbers.Entries[preferred]
          : phoneNumbers.Entries.Create(preferred);
        entry.StreetLine1 = address;
      });
    }
  }
}
