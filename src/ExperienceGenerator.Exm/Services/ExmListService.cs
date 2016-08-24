using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.ExM.Framework.Diagnostics;
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

    internal void WaitUntilListsUnlocked()
    {
      bool hasUnlocked;

      _specification.Job.Status = "Waiting for lists to unlock...";

      var tries = 0;
      do
      {
        hasUnlocked = false;
        tries++;

        var lockedLists = new List<string>();
        foreach (var list in ListManager.GetAll())
        {
          if (ListManager.IsLocked(list) || ListManager.IsInUse(list))
          {
            if (tries > _specification.ListUnlockedAttempts)
            {
              UnlockList(list);
            }
            else
            {
              hasUnlocked = true;
              lockedLists.Add(list.Name);
            }
          }
        }

        if (hasUnlocked)
        {
          _specification.Job.Status = string.Format(
              "Waiting for lists to unlock {0}/{1} ({2})...",
              tries,
              _specification.ListUnlockedAttempts,
              string.Join(", ", lockedLists));
          Thread.Sleep(1000);
        }
      } while (hasUnlocked);
    }

    private void UnlockList(ContactList list)
    {
      if (!ListManager.IsLocked(list) && !ListManager.IsInUse(list))
      {
        return;
      }

      Logger.Instance.LogWarn($"Force unlocking list {list.Id} - {list.DisplayName}");
      var lockTest = ListManager.GetLock(list);
      ListManager.Unlock(lockTest);
    }
  }
}