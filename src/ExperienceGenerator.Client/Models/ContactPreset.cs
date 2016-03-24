namespace ExperienceGenerator.Client.Models
{
  using Newtonsoft.Json.Linq;

  public class ContactPreset
  {
    public string Name { get; set; }

    public JArray Spec { get; set; }
  }
}