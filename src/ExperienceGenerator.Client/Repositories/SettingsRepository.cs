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

  public class SettingsRepository
  {
    private const string presetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Presets";

    protected Database Database => Sitecore.Configuration.Factory.GetDatabase("core");

    protected Item PresetsRoot => this.Database.GetItem(presetsRootPath);
    public void Save(string name, JObject spec)
    {
      
      var templateId = Templates.Preset.ID;

      var presetRoot = this.SitePresetRoot;

      var presetItem = presetRoot.Add(name, new TemplateID(templateId));

      presetItem.Editing.BeginEdit();
      presetItem[Templates.Preset.Fields.Specification] = spec.ToString();
      presetItem.Editing.EndEdit();
    }

    protected Item CreateSiteFolder(Item root)
    {
      var siteName = Sitecore.Context.Site.Name;
      var presetRoot = root.Add(siteName, new TemplateID(Sitecore.TemplateIDs.Folder));
      return presetRoot;
    }

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

    public List<string> GetPresetsIds()
    {
      return this.GetPresets().Select(child => child.ID.ToString()).ToList();
    }

    public string  GetPresetsQuery()
    {
      return $"{this.SitePresetRoot.Paths.FullPath}//*";
    }
  }
}