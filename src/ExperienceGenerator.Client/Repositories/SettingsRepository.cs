using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExperienceGenerator.Client.Repositories
{
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
      var presetRoot = this.Database.GetItem(presetsRootPath);

      if (presetRoot == null)
      {
        return;
      }

      var presetItem = presetRoot.Add(name, new TemplateID(templateId));

      presetItem.Editing.BeginEdit();
      presetItem[Templates.Preset.Fields.Specification] = spec.ToString();
      presetItem.Editing.EndEdit();
    }

    public JObject GetSettingPreset(ID id)
    {
      var preset = this.PresetsRoot.Children[id];
      if (preset == null)
      {
        return null;
      }

      return this.Create(preset);
    }

    public List<Item> GetPresets()
    {
      return this.PresetsRoot.Children.ToList();
    }

    protected JObject Create(Item preset)
    {
      return JObject.Parse(preset[Templates.Preset.Fields.Specification]);
    }

    public List<string> GetPresetsIds()
    {
      return this.GetPresets().Select(child => child.ID.ToString()).ToList();
    }
  }
}