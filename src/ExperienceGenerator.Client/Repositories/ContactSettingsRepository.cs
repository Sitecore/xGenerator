using Sitecore.ContentSearch.Utilities;

namespace ExperienceGenerator.Client.Repositories
{
  using Newtonsoft.Json.Linq;
  using Sitecore.Data;
  using Sitecore.Data.Items;

  public class ContactSettingsRepository : BaseSettingsRepository
  {
    public ContactSettingsRepository() : base()
    {

    }
    public ContactSettingsRepository(Database database) : base(database)
    {

    }
    private const string contactPresetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts";
    protected override Item PresetsRoot => this.Database.GetItem(contactPresetsRootPath);
    
    public JArray GetContactSettingPreset(ID id)
    {
      var preset = this.PresetsRoot.Axes.SelectSingleItem($"//*[@@id='{id}']");
      return preset == null ? null : this.CreateArray(preset);
    }
  }
}