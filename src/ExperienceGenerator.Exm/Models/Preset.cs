using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Exm.Models
{
    public class Preset
    {
        public string Name { get; set; }

        public JObject Spec { get; set; }
    }
}