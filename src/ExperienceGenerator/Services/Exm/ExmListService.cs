using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExperienceGenerator.Models.Exm;
using Sitecore.Configuration;
using Sitecore.ListManagement;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.ListManagement.Services;

namespace ExperienceGenerator.Services.Exm
{
    public class ExmListService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly ExmContactService _contactService;
        private readonly List<ContactList> _lists = new List<ContactList>();
        private readonly Random _random = new Random();

        public ListManager<ContactList, ContactData> ListManager { get; }

        public ExmListService(ExmDataPreparationModel specification, ExmContactService contactService)
        {
            _specification = specification;
            _contactService = contactService;
            ListManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
        }

        public void CreateLists()
        {
            if (_specification.SpecificLists != null && _specification.SpecificLists.Any())
            {
                CreateSpecificLists();
            }
            else if (_specification.RandomLists != null)
            {
                CreateRandomLists();
            }

            WaitUntilListsUnlocked();
        }

        public ContactList GetList(string name)
        {
            return _lists.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void CreateRandomLists()
        {
            for (var i = 0; i < _specification.RandomLists.NumLists; i++)
            {
                var name = "Auto List " + i;
                var xaList = CreateList(name);
                ListManager.AssociateContacts(xaList, _contactService.SelectRandomContacts(_specification.RandomLists.ContactsMin, _specification.RandomLists.ContactsMax));
                _lists.Add(xaList);
            }
        }

        private void CreateSpecificLists()
        {
            foreach (var listSpecification in _specification.SpecificLists)
            {
                var xaList = CreateList(listSpecification.Name);
                ListManager.AssociateContacts(xaList, _contactService.SelectRandomContacts(listSpecification.NumContacts));
                _lists.Add(xaList);
            }
        }

        private ContactList CreateList(string name)
        {
            var contactList = new ContactList
            {
                Name = name,
                Owner = "xGenerator",
                Type = ListRowType.ContactList
            };

            ListManager.Create(contactList);
            _specification.Job.CompletedLists++;
            return contactList;
        }

        private void WaitUntilListsUnlocked()
        {
            bool hasUnlocked;

            _specification.Job.Status = "Waiting for lists to unlock...";

            do
            {
                hasUnlocked = false;

                var lockedLists = new List<string>();
                foreach (var list in _lists)
                {
                    if (ListManager.IsLocked(list))
                    {
                        hasUnlocked = true;
                        lockedLists.Add(list.Name);
                    }
                }

                if (hasUnlocked)
                {
                    _specification.Job.Status = string.Format(
                        "Waiting for lists to unlock ({0})...",
                        string.Join(", ", lockedLists));
                    Thread.Sleep(1000);
                }
            } while (hasUnlocked);
        }

        public void GrowLists()
        {
            if (_specification.ContactGrowth < 0.01m)
            {
                return;
            }

            var percentage = _specification.ContactGrowth / 100m;
            var contactsToCreate = (int)(_contactService.ContactCount * percentage);

            _specification.Job.Status = string.Format("Creating {0} extra contacts", contactsToCreate);

            var contactsPerList = new Dictionary<string, List<ContactData>>();
            var listNames = _lists.Select(x => x.Name).ToList();
            foreach (var list in _lists)
            {
                contactsPerList[list.Name] = new List<ContactData>();
            }

            for (var i = 0; i < contactsToCreate; i++)
            {
                var contact = _contactService.CreateContact(i);
                var contactData = _contactService.ContactToContactData(contact);

                var numListsToTake = _random.Next(1, _lists.Count);
                var includeLists = listNames.OrderBy(x => Guid.NewGuid()).Take(numListsToTake);

                foreach (var listName in includeLists)
                {
                    contactsPerList[listName].Add(contactData);
                }
            }

            WaitUntilListsUnlocked();

            foreach (var list in _lists)
            {
                ListManager.AssociateContacts(list, contactsPerList[list.Name]);
            }

            WaitUntilListsUnlocked();
        }
    }
}