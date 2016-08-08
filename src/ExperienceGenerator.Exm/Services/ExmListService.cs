namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Linq;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.ListManagement;
  using Sitecore.ListManagement.ContentSearch.Model;

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
  }
}