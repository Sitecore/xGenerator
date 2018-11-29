using Newtonsoft.Json.Linq;

namespace ExperienceGenerator.Client.Models
{

  public class ContactPreset
  {
    public string Name { get; set; }

    public JArray Spec { get; set; }
  }
}
