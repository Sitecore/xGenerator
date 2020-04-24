using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace ExperienceGenerator.Exm.Repositories
{
    public class SettingsRepository
    {
        public SettingsRepository(Database database)
        {
            Database = database;
        }

        public SettingsRepository() : this(Factory.GetDatabase("core"))
        {
        }

        private const string presetsRootPath = "/sitecore/client/Applications/ExmExperienceGenerator/Common/Presets";
        protected virtual Item PresetsRoot => Database.GetItem(presetsRootPath);
        protected Database Database { get; set; }

        protected virtual Item SitePresetRoot
        {
            get
            {
                var siteName = Context.Site.Name;
                var presetPath = $"{PresetsRoot.Paths.FullPath}/{siteName}";
                var presetRoot = Database.GetItem(presetPath);
                if (presetRoot == null)
                {
                    return CreateSiteFolder(PresetsRoot);
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

            var presetRoot = SitePresetRoot;

            using (new SecurityDisabler())
            {
                var presetItem = presetRoot.Children.FirstOrDefault(x => x.Name == name) ?? presetRoot.Add(name, new TemplateID(templateId));

                presetItem.Editing.BeginEdit();
                presetItem[Templates.Preset.Fields.Specification] = spec;
                presetItem.Editing.EndEdit();
            }
        }

        protected Item CreateSiteFolder(Item root)
        {
            var siteName = Context.Site.Name;
            var presetRoot = root.Add(siteName, new TemplateID(TemplateIDs.Folder));
            return presetRoot;
        }


        public JObject GetSettingPreset(ID id)
        {
            var preset = SitePresetRoot.Axes.SelectSingleItem($"//*[@@id='{id}']");
            if (preset == null)
            {
                return null;
            }

            return Create(preset);
        }

        public List<Item> GetPresets()
        {
            return SitePresetRoot.Children.ToList();
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
            return GetPresets().Select(child => child.ID.ToString()).ToList();
        }

        public string GetPresetsQuery()
        {
            return $"{SitePresetRoot.Paths.FullPath}//*";
        }
    }
}
