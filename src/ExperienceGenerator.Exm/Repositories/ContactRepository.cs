using System;
using System.Collections.Generic;
using ExperienceGenerator.Exm.Infrastructure;
using ExperienceGenerator.Exm.Models;
using Faker;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.Configuration;
using Sitecore.XConnect.Collection.Model;

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

        private XConnectClient GetXConnectClient()
        {
            return SitecoreXConnectClientConfiguration.GetClient();
        }

        public Sitecore.Analytics.Tracking.Contact GetContact(Guid id)
        {
            return _contactRepository.LoadContact(id);
        }

        private Contact CreateContact()
        {
            var identifier = CreateUniqueIdentifier();

            using (var client = GetXConnectClient())
            {
                var xGenContact = new Contact(identifier);
                
                client.AddContact(xGenContact);

                var contactPersonalInfo = new PersonalInformation()
                {
                    FirstName = Name.First(),
                    LastName = Name.Last()
                };
                client.SetFacet(xGenContact, PersonalInformation.DefaultFacetKey, contactPersonalInfo);

                var contactEmailAddresses = new EmailAddressList(new EmailAddress(Internet.Email($"{contactPersonalInfo.FirstName} {contactPersonalInfo.LastName}"), true),"Work");
                client.SetFacet(xGenContact, EmailAddressList.DefaultFacetKey, contactEmailAddresses);
                
                client.Submit();

                xGenContact = client.Get<Contact>(new IdentifiedContactReference(identifier.Source, identifier.Identifier),
                    new ContactExecutionOptions());

                return xGenContact;
            }
        }

        private ContactIdentifier CreateUniqueIdentifier()
        {
            const string source = "ExperienceGenerator";

            var identValue = "xGen_" + ShortGuid.NewGuid();

            var identifier = new ContactIdentifier(source,identValue, ContactIdentifierType.Known);
            
            var contact = _contactRepository.LoadContact(identifier.Source, identifier.Identifier);

            if (contact != null)
            {
                identifier = CreateUniqueIdentifier();
            }
            return identifier;
        }
    }
}
