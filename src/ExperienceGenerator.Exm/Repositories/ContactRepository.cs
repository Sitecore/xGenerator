using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Exm.Infrastructure;
using ExperienceGenerator.Exm.Models;
using Faker;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Data;
using Sitecore.ListManagement.ContentSearch.Model;

namespace ExperienceGenerator.Exm.Repositories
{
    public class ContactRepository
    {
        private readonly Sitecore.Analytics.Data.ContactRepository _contactRepository;

        public ContactRepository()
        {
            _contactRepository = new Sitecore.Analytics.Data.ContactRepository();
        }

        public IEnumerable<Contact> CreateContacts(Job job, int numContacts)
        {
            var addedContacts = new List<Contact>();
            for (var i = 0; i < numContacts; i++)
            {
                addedContacts.Add(CreateContact());
            }
            return addedContacts;
        }

        public Contact GetContact(Guid id)
        {
            return _contactRepository.LoadContactReadOnly(id);
        }

        private Contact CreateContact()
        {
            var identifier = CreateUniqueIdentifier();

            var contact = _contactRepository.CreateContact(ID.NewID);
            contact.Identifiers.AuthenticationLevel = AuthenticationLevel.None;
            contact.System.Classification = 0;
            contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
            contact.Identifiers.Identifier = identifier;
            contact.System.OverrideClassification = 0;
            contact.System.Value = 0;
            contact.System.VisitCount = 0;
/*
            var contactPreferences = contact.GetFacet<IContactPreferences>("Preferences");
            contactPreferences.Language = _languages;
*/
            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            contactPersonalInfo.FirstName = Name.First();
            contactPersonalInfo.Surname = Name.Last();

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            contactEmailAddresses.Entries.Create("Work").SmtpAddress = Internet.Email($"{contactPersonalInfo.FirstName} {contactPersonalInfo.Surname}");
            contactEmailAddresses.Preferred = "Work";

            var leaseOwner = new LeaseOwner("CONTACT_CREATE", LeaseOwnerType.OutOfRequestWorker);
            var options = new ContactSaveOptions(true, leaseOwner, null);
            _contactRepository.SaveContact(contact, options);

            return contact;
        }

        private string CreateUniqueIdentifier()
        {
            var identifier = "xGen_" + ShortGuid.NewGuid();

            var contact = _contactRepository.LoadContactReadOnly(identifier);
            while (contact != null)
            {
                identifier = "xGen_" + ShortGuid.NewGuid();
                contact = _contactRepository.LoadContactReadOnly(identifier);
            }
            return identifier;
        }
    }
}
