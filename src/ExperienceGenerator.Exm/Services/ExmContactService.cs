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
  }
}