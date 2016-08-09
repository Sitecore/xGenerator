namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using Sitecore.Analytics.Model.Entities;
  using Sitecore.Analytics.Tracking;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.ListManagement;
  using Sitecore.ListManagement.ContentSearch.Model;
  using Sitecore.ListManagement.Services;

  public class ExmListService
  {
    public ListManager<ContactList, ContactData> ListManager { get; }

    public ExmListService()
    {
      this.ListManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
    }

    public ContactList GetList(string name)
    {
      return this.ListManager.GetAll().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public ContactList GetList(ID id)
    {
      return this.ListManager.FindById(id.ToShortID().ToString());
    }

    public ContactList CreateList(string s, IEnumerable<Contact> addlContacts)
    {
      var contactList = new ContactList
      {
        Name = s,
        Owner = "xGenerator",
        Type = ListRowType.ContactList
      };

      this.ListManager.Create(contactList);
      this.ListManager.AssociateContacts(contactList, addlContacts.Select(ContactToContactData));


      while (this.ListManager.IsLocked(contactList))
      {
        Thread.Sleep(1000);
      }

      return contactList;
    }


    public Sitecore.ListManagement.ContentSearch.Model.ContactData ContactToContactData(Contact contact)
    {
      var result = new Sitecore.ListManagement.ContentSearch.Model.ContactData
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

    public IEnumerable<ContactData> GetContacts(ContactList xaList)
    {
      return this.ListManager.GetContacts(xaList).ToList();
    }
  }
}