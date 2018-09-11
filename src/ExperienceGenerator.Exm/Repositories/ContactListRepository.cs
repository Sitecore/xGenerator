using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Exm.Infrastructure;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Services;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.ListManagement;
using Sitecore.ListManagement.Services;
using Sitecore.Modules.EmailCampaign.ListManager;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Repositories
{
    public class ContactListRepository
    {
        //TODO: Make This into Sitecore 9 Contact List Friendly


        private const string DefaultListManagerOwner = "xGenerator";

        private readonly ListManager<ContactList, ContactData> _listManager;
        private readonly UnlockListService _unlockListService;

        public ContactListRepository()
        {
            _listManager = (ListManager<ContactList, ContactData>) Factory.CreateObject("contactListManager", false);
            _unlockListService = new UnlockListService();
        }


        public ContactList GetList(ID id)
        {
            return _listManager.FindById(id.ToShortID().ToString());
        }

        public ContactList CreateList(Job job, string name, IEnumerable<Contact> addContacts, string listManagerOwner = DefaultListManagerOwner)
        {
            job.Status = $"Creating List {name}";

            var contactList = new ContactList
                              {
                                  Name = name,
                                  Owner = listManagerOwner,
                                  Type = ListRowType.ContactList
                              };

            _listManager.Create(contactList);
            _listManager.AssociateContacts(contactList, addContacts.Select(MapContactToContactData));
            job.CompletedLists++;

            _unlockListService.UnlockList(job, contactList);

            return contactList;
        }

        private ContactData MapContactToContactData(Contact contact)
        {
            var result = new ContactData
                         {
                             ContactId = contact.ContactId,
                             Identifier = contact.Identifiers.Identifier
                         };

            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            result.FirstName = contactPersonalInfo.FirstName;
            result.MiddleName = contactPersonalInfo.MiddleName;
            result.Surname = contactPersonalInfo.Surname;
            result.Nickname = contactPersonalInfo.Nickname;

            if (contactPersonalInfo.BirthDate != null)
            {
                result.BirthDate = contactPersonalInfo.BirthDate.Value;
            }

            result.Gender = contactPersonalInfo.Gender;
            result.JobTitle = contactPersonalInfo.JobTitle;
            result.Suffix = contactPersonalInfo.Suffix;
            result.Title = contactPersonalInfo.Title;

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            result.PreferredEmail = contactEmailAddresses.Entries[contactEmailAddresses.Preferred].SmtpAddress;

            result.IdentificationLevel = contact.Identifiers.IdentificationLevel.ToString();
            result.Classification = contact.System.Classification;
            result.VisitCount = contact.System.VisitCount;
            result.Value = contact.System.Value;
            result.IntegrationLabel = contact.System.IntegrationLabel;

            return result;
        }


        public IEnumerable<ContactData> GetContacts(Job job, ContactList xaList)
        {
            return _listManager.GetContacts(xaList).ToList();
        }


        public List<ContactData> GetContacts(MessageItem message, IEnumerable<Guid> excludeContacts)
        {
            var lists = message.RecipientManager.IncludedRecipientListIds;
            var contactsForThisEmail = new List<ContactData>();

            foreach (var listId in lists)
            {
                var list = GetList(listId);
                if (list == null)
                    continue;
                var contacts = _listManager.GetContacts(list);
                contactsForThisEmail.AddRange(contacts);
            }

            contactsForThisEmail = contactsForThisEmail.DistinctBy(x => x.ContactId).Where(x => !excludeContacts.Contains(x.ContactId)).ToList();

            return contactsForThisEmail;
        }

        public bool Exists(ID contactListID)
        {
            return GetList(contactListID) != null;
        }
    }
}
