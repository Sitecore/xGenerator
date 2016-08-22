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
using ContactData = Sitecore.ListManagement.ContentSearch.Model.ContactData;

namespace ExperienceGenerator.Exm.Services
{
  public class ExmContactService
  {
    private readonly ExmJobDefinitionModel _specification;
    private readonly string[] _languages = { "en", "uk" };
    private readonly List<Contact> _contacts = new List<Contact>();

    public int ContactCount { get; private set; }

    public ExmContactService(ExmJobDefinitionModel specification)
    {
      _specification = specification;
    }

    public IEnumerable<Contact> CreateContacts(int numContacts)
    {
      for (var i = 0; i < numContacts; i++)
      {
        yield return CreateContact();
      }
    }

    public void AddContacts(List<ContactData> contactDataList)
    {
      var contactRepository = new ContactRepository();
      _contacts.AddRange(contactDataList.Select(x => contactRepository.LoadContactReadOnly(x.Identifier)));
    }

    public Contact GetContact(Guid id)
    {
      return _contacts.FirstOrDefault(x => x.ContactId == id);
    }

    public Contact CreateContact()
    {
      var identifier = "XGen" + ContactCount;

      var contactRepository = new ContactRepository();

      var contact = contactRepository.LoadContactReadOnly(identifier);
      if (contact != null)
      {
        DoContactCreated(contact);
        return contact;
      }

      contact = contactRepository.CreateContact(ID.NewID);
      contact.Identifiers.AuthenticationLevel = AuthenticationLevel.None;
      contact.System.Classification = 0;
      contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
      contact.Identifiers.Identifier = "XGen" + ContactCount;
      contact.System.OverrideClassification = 0;
      contact.System.Value = 0;
      contact.System.VisitCount = 0;

      var contactPreferences = contact.GetFacet<IContactPreferences>("Preferences");
      contactPreferences.Language = _languages[ContactCount % _languages.Length];

      var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
      contactPersonalInfo.FirstName = Faker.Name.First();
      contactPersonalInfo.Surname = Faker.Name.Last();

      var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
      contactEmailAddresses.Entries.Create("Work").SmtpAddress =
      Faker.Internet.Email($"{contactPersonalInfo.FirstName} {contactPersonalInfo.Surname}");
      contactEmailAddresses.Preferred = "Work";

      var leaseOwner = new LeaseOwner("CONTACT_CREATE", LeaseOwnerType.OutOfRequestWorker);
      var options = new ContactSaveOptions(true, leaseOwner, null);
      contactRepository.SaveContact(contact, options);

      DoContactCreated(contact);
      return contact;
    }

    private void DoContactCreated(Contact contact)
    {
      _contacts.Add(contact);
      _specification.Job.CompletedContacts++;
      ContactCount++;
    }
  }
}