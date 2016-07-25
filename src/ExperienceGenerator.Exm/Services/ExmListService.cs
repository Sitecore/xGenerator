namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using ExperienceGenerator.Exm.Models;
  using Sitecore.Configuration;
  using Sitecore.ListManagement;
  using Sitecore.ListManagement.ContentSearch.Model;
  using Sitecore.ListManagement.Services;

  public class ExmListService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly ExmContactService _contactService;
        private readonly List<ContactList> _lists = new List<ContactList>();
        private readonly Random _random = new Random();

        public ListManager<ContactList, ContactData> ListManager { get; }

        public ExmListService(ExmDataPreparationModel specification, ExmContactService contactService)
        {
            this._specification = specification;
            this._contactService = contactService;
            this.ListManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
        }

        public void CreateLists()
        {
            if (this._specification.SpecificLists != null && this._specification.SpecificLists.Any())
            {
                this.CreateSpecificLists();
            }
            else if (this._specification.RandomLists != null)
            {
                this.CreateRandomLists();
            }

            this.WaitUntilListsUnlocked();
        }

        public ContactList GetList(string name)
        {
            return this._lists.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void CreateRandomLists()
        {
            for (var i = 0; i < this._specification.RandomLists.NumLists; i++)
            {
                var name = "Auto List " + i;
                var xaList = this.CreateList(name);
                this.ListManager.AssociateContacts(xaList, this._contactService.SelectRandomContacts(this._specification.RandomLists.ContactsMin, this._specification.RandomLists.ContactsMax));
                this._lists.Add(xaList);
            }
        }

        private void CreateSpecificLists()
        {
            foreach (var listSpecification in this._specification.SpecificLists)
            {
                var xaList = this.CreateList(listSpecification.Name);
                this.ListManager.AssociateContacts(xaList, this._contactService.SelectRandomContacts(listSpecification.NumContacts));
                this._lists.Add(xaList);
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

            this.ListManager.Create(contactList);
            this._specification.Job.CompletedLists++;
            return contactList;
        }

        private void WaitUntilListsUnlocked()
        {
            bool hasUnlocked;

            this._specification.Job.Status = "Waiting for lists to unlock...";

            do
            {
                hasUnlocked = false;

                var lockedLists = new List<string>();
                foreach (var list in this._lists)
                {
                    if (this.ListManager.IsLocked(list))
                    {
                        hasUnlocked = true;
                        lockedLists.Add(list.Name);
                    }
                }

                if (hasUnlocked)
                {
                    this._specification.Job.Status = string.Format(
                        "Waiting for lists to unlock ({0})...",
                        string.Join(", ", lockedLists));
                    Thread.Sleep(1000);
                }
            } while (hasUnlocked);
        }

        public void GrowLists()
        {
            if (this._specification.ContactGrowth < 0.01m)
            {
                return;
            }

            var percentage = this._specification.ContactGrowth / 100m;
            var contactsToCreate = (int)(this._contactService.ContactCount * percentage);

            this._specification.Job.Status = string.Format("Creating {0} extra contacts", contactsToCreate);

            var contactsPerList = new Dictionary<string, List<ContactData>>();
            var listNames = this._lists.Select(x => x.Name).ToList();
            foreach (var list in this._lists)
            {
                contactsPerList[list.Name] = new List<ContactData>();
            }

            for (var i = 0; i < contactsToCreate; i++)
            {
                var contact = this._contactService.CreateContact(i);
                var contactData = this._contactService.ContactToContactData(contact);

                var numListsToTake = this._random.Next(1, this._lists.Count);
                var includeLists = listNames.OrderBy(x => Guid.NewGuid()).Take(numListsToTake);

                foreach (var listName in includeLists)
                {
                    contactsPerList[listName].Add(contactData);
                }
            }

            this.WaitUntilListsUnlocked();

            foreach (var list in this._lists)
            {
                this.ListManager.AssociateContacts(list, contactsPerList[list.Name]);
            }

            this.WaitUntilListsUnlocked();
        }
    }
}