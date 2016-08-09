namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using ExperienceGenerator.Exm.Models;
  using Sitecore.Analytics.Data;
  using Sitecore.Analytics.DataAccess;
  using Sitecore.Analytics.Model;
  using Sitecore.Analytics.Model.Entities;
  using Sitecore.Analytics.Tracking;
  using Sitecore.Data;

  public class ExmContactService
  {
    private readonly string[] _languages = { "en", "uk" };
    private readonly Random _random = new Random();
    private readonly List<Contact> _contacts = new List<Contact>();

    public int ContactCount { get; private set; }

    public Contact GetContact(Guid id)
    {

      var contactRepository = new ContactRepository();

      var contact = contactRepository.LoadContactReadOnly(id);

      return contact;

    }

    public IEnumerable<Contact> CreateContacts(int numContacts)
    {
      for (var i = 0; i < numContacts; i++)
      {
        yield return CreateContact();
      }
    }



    public Contact CreateContact()
    {
      var identifier = "XGen" + Guid.NewGuid();

      var contactRepository = new ContactRepository();

      var contact = contactRepository.LoadContactReadOnly(identifier);
      if (contact != null)
      {
        return contact;
      }

      contact = contactRepository.CreateContact(ID.NewID);
      contact.Identifiers.AuthenticationLevel = AuthenticationLevel.None;
      contact.System.Classification = 0;
      contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
      contact.Identifiers.Identifier = identifier;
      contact.System.OverrideClassification = 0;
      contact.System.Value = 0;
      contact.System.VisitCount = 0;

      var contactPreferences = contact.GetFacet<IContactPreferences>("Preferences");
      contactPreferences.Language = this._languages[DateTime.Now.Second % this._languages.Length];

      var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
      contactPersonalInfo.FirstName = Faker.Name.First();
      contactPersonalInfo.Surname = Faker.Name.Last();

      var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
      contactEmailAddresses.Entries.Create("Work").SmtpAddress =
          Faker.Internet.Email(string.Format("{0} {1}", contactPersonalInfo.FirstName, contactPersonalInfo.Surname));
      contactEmailAddresses.Preferred = "Work";

      var leaseOwner = new LeaseOwner("CONTACT_CREATE", LeaseOwnerType.OutOfRequestWorker);
      var options = new ContactSaveOptions(true, leaseOwner, null);
      contactRepository.SaveContact(contact, options);

      return contact;
    }

  }
}