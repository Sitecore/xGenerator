namespace Colossus.Integration.Processing
{
  using System;
  using System.IO;
  using Colossus.Web;
  using Sitecore;
  using Sitecore.Analytics.Model.Entities;
  using Sitecore.Analytics.Tracking;
  using Sitecore.Data.Items;

  public class ContactDataProcessor : ISessionPatcher
  {
    public void UpdateSession(Session session, RequestInfo requestInfo)
    {
      requestInfo.SetIfVariablePresent("ContactId", session.Identify);

      UpdatePersonalInformation(session, requestInfo);

      UpdatePicture(session, requestInfo);

      UpdateContacts(session, requestInfo);
    }

    private static void UpdatePersonalInformation(Session session, RequestInfo requestInfo)
    {
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

      requestInfo.SetIfVariablePresent("ContactBirthDate", date =>
      {
        if (string.IsNullOrEmpty(date))
        {
          return;
        }

        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.BirthDate = DateTime.ParseExact(date.Substring(0, 8), "yyyyMMdd", null);
      });

      requestInfo.SetIfVariablePresent("ContactGender", gender =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.Gender = gender;
      });

      requestInfo.SetIfVariablePresent("ContactJobTitle", title =>
      {
        var personalInfo = session.Contact.GetFacet<IContactPersonalInfo>("Personal");
        personalInfo.JobTitle = title;
      });
    }

    private static void UpdateContacts(Session session, RequestInfo requestInfo)
    {
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
        var addresses = session.Contact.GetFacet<IContactAddresses>("Addresses");
        var preferred = "Primary";
        addresses.Preferred = preferred;
        var entry = addresses.Entries.Contains(preferred)
          ? addresses.Entries[preferred]
          : addresses.Entries.Create(preferred);
        entry.StreetLine1 = address;
      });
    }

    private static void UpdatePicture(Session session, RequestInfo requestInfo)
    {
      requestInfo.SetIfVariablePresent("ContactPicture", pictureItemID =>
      {
        if (string.IsNullOrEmpty(pictureItemID))
        {
          return;
        }

        var item = (Context.ContentDatabase ?? Context.Database).GetItem(pictureItemID);
        if (item == null)
        {
          return;
        }

        var mItem = new MediaItem(item);


        var picture = session.Contact.GetFacet<IContactPicture>("Picture");

        using (var ms = new MemoryStream())
        {
          mItem.GetMediaStream()?.CopyTo(ms);
          picture.Picture = ms.ToArray();
        }

        picture.MimeType = mItem.MimeType;
      });
    }
  }
}