using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExperienceGenerator.Client.Repositories
{
  using System.Web.Http;
  using Newtonsoft.Json.Linq;
  using Sitecore.Data;
  using Sitecore.Data.Items;

  public class BaseSettingsRepository
  {
    private const string presetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Presets";
    protected virtual Item PresetsRoot => this.Database.GetItem(presetsRootPath);
    protected Database Database => Sitecore.Configuration.Factory.GetDatabase("core");

    protected Item SitePresetRoot
    {
      get
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
    public void Save(string name, JObject spec)
    {

      Save(name, spec.ToString());

    }



    public void Save(string name, JArray spec)
    {

      Save(name, spec.ToString());
    }
    private void Save(string name, string spec)
    {

      var templateId = Templates.Preset.ID;

      var presetRoot = this.SitePresetRoot;

      var presetItem = presetRoot.Add(name, new TemplateID(templateId));

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
      var preset = this.SitePresetRoot.Children[id];
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
  public class SettingsRepository : BaseSettingsRepository
  {

  }
}