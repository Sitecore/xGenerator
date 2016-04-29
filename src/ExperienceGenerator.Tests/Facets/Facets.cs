namespace ExperienceGenerator.Tests.Facets
{
  using Sitecore.Analytics.Model.Entities;
  using Sitecore.Analytics.Model.Framework;

  internal class ContactAddresses : Facet, IContactAddresses
  {
    public IElementDictionary<IAddress> Entries => this.GetDictionary<IAddress>("Entries");

    public string Preferred { get; set; }

    public ContactAddresses()
    {
      this.EnsureAttribute<string>("Preferred");
      this.EnsureDictionary<IAddress>("Entries");
    }
  }

  internal class ContactEmails : Facet, IContactEmailAddresses
  {
    public IElementDictionary<IEmailAddress> Entries => this.GetDictionary<IEmailAddress>("Entries");

    public string Preferred { get; set; }

    public ContactEmails()
    {
      this.EnsureAttribute<string>("Preferred");
      this.EnsureDictionary<IEmailAddress>("Entries");
    }
  }


  internal class ContactPhones : Facet, IContactPhoneNumbers
  {
    public IElementDictionary<IPhoneNumber> Entries => this.GetDictionary<IPhoneNumber>("Entries");
    public string Preferred { get; set; }

    public ContactPhones()
    {
      this.EnsureAttribute<string>("Preferred");
      this.EnsureDictionary<IPhoneNumber>("Entries");
    }
  }
}