namespace ExperienceGenerator.Client.Repositories
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.SecurityModel;

    public class SettingsRepository
  {

    public SettingsRepository(Database database)
    {
      this.Database = database;
    }

    public SettingsRepository() : this(Sitecore.Configuration.Factory.GetDatabase("core"))
    {
    }

    private const string presetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Presets";
    protected virtual Item PresetsRoot => this.Database.GetItem(presetsRootPath);
    protected Database Database { get; set; }

    protected virtual Item SitePresetRoot
    {
      get
      {
        using (new SecurityDisabler())
        {
            var siteName = Sitecore.Context.Site.Name;
            var presetPath = $"{this.PresetsRoot.Paths.FullPath}/{siteName}";
            var presetRoot = this.Database.GetItem(presetPath);
            if (presetRoot == null)
            {
                return this.CreateSiteFolder(this.PresetsRoot);
            }
            return presetRoot;
        }
      }
    }

    public void Save(string name, JObject spec)
    {
      this.Save(name, spec.ToString());
    }

    public void Save(string name, JArray spec)
    {
      this.Save(name, spec.ToString());
    }
    private void Save(string name, string spec)
    {
      var templateId = Templates.Preset.ID;

      var presetRoot = this.SitePresetRoot;
     
      var presetItem = presetRoot.Children.FirstOrDefault(x => x.Name == name)??presetRoot.Add(name, new TemplateID(templateId));

      presetItem.Editing.BeginEdit();
      presetItem[Templates.Preset.Fields.Specification] = spec;
      presetItem.Editing.EndEdit();
    }

    protected Item CreateSiteFolder(Item root)
    {
      var siteName = Sitecore.Context.Site.Name;
      var presetRoot = root.Add(siteName, new TemplateID(Sitecore.TemplateIDs.Folder));
      return presetRoot;
    }

    public JObject GetSettingPreset(ID id)
    {
      var preset = this.SitePresetRoot.Axes.SelectSingleItem($"//*[@@id='{id}']");
      if (preset == null)
      {
        return null;
      }

      return this.Create(preset);
    }

    public List<Item> GetPresets()
    {
      return this.SitePresetRoot.Children.ToList();
    }

    protected JObject Create(Item preset)
    {
      return JObject.Parse(preset[Templates.Preset.Fields.Specification]);
    }
    protected JArray CreateArray(Item preset)
    {
      return JArray.Parse(preset[Templates.Preset.Fields.Specification]);
    }

    public List<string> GetPresetsIds()
    {
      return this.GetPresets().Select(child => child.ID.ToString()).ToList();
    }

    public string GetPresetsQuery()
    {
      return $"{this.SitePresetRoot.Paths.FullPath}//*";
    }

  }
}
