namespace ExperienceGenerator.Client.Repositories
{
  using Newtonsoft.Json.Linq;
  using Sitecore.Data;
  using Sitecore.Data.Items;

  public class ContactSettingsRepository : SettingsRepository
  {
    public ContactSettingsRepository() : base()
    {

    }
    public ContactSettingsRepository(Database database) : base(database)
    {

    }
    private const string contactPresetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts";
    protected override Item PresetsRoot => this.Database.GetItem(contactPresetsRootPath);
    protected override Item SitePresetRoot => this.PresetsRoot;

    public JArray GetContactSettingPreset(ID id)
    {
      var preset = this.SitePresetRoot.Children[id];
      return preset == null ? null : this.CreateArray(preset);
    }
  }
}