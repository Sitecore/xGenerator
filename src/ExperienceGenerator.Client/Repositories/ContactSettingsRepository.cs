namespace ExperienceGenerator.Client.Repositories
{
  using Newtonsoft.Json.Linq;
  using Sitecore.Data;
  using Sitecore.Data.Items;

  public class ContactSettingsRepository : BaseSettingsRepository
  {
    private const string presetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts";
    protected override Item PresetsRoot => this.Database.GetItem(presetsRootPath);
    public JArray GetContactSettingPreset(ID id)
    {
      var preset = this.SitePresetRoot.Children[id];
      if (preset == null)
      {
        return null;
      }

      return this.CreateArray(preset);
    }
  }
}