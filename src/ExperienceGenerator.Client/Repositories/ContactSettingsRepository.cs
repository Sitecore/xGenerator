using Newtonsoft.Json.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace ExperienceGenerator.Client.Repositories
{
    public class ContactSettingsRepository : SettingsRepository
    {
        public ContactSettingsRepository()
        {
        }

        public ContactSettingsRepository(Database database) : base(database)
        {
        }

        private const string contactPresetsRootPath = "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts";
        protected override Item PresetsRoot => Database.GetItem(contactPresetsRootPath);

        public JArray GetContactSettingPreset(ID id)
        {
            using (new SecurityDisabler())
            {
                var preset = PresetsRoot.Axes.SelectSingleItem($"//*[@@id='{id}']");
                return preset == null ? null : CreateArray(preset);
            }
        }
    }
}
