using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Exm.Models;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.ListManagement;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.ListManagement.Services;

namespace ExperienceGenerator.Exm.Services
{
	public class ExmListService
	{
		private readonly ExmJobDefinitionModel _specification;
		
		public ListManager<ContactList, ContactData> ListManager { get; }

		public ExmListService(ExmJobDefinitionModel specification)
		{
			_specification = specification;
			ListManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
		}

		
		public ContactList GetList(string name)
		{
			return ListManager.GetAll().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}

    public ContactList GetList(ID id)
    {
      return ListManager.FindById(id.ToShortID().ToString());
    }
    
    public ContactList CreateList(string name, IEnumerable<Contact> addlContacts)
		{
			var contactList = new ContactList
			{
				Name = name,
				Owner = "xGenerator",
				Type = ListRowType.ContactList
			};

			ListManager.Create(contactList);
      ListManager.AssociateContacts(contactList, addlContacts.Select(ContactToContactData));
      _specification.Job.CompletedLists++;
			return contactList;
		}

    public ContactData ContactToContactData(Contact contact)
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


    public IEnumerable<ContactData> GetContacts(ContactList xaList)
    {
      return ListManager.GetContacts(xaList).ToList();
    }
  }
}