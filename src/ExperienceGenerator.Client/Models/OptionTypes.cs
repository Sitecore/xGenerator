namespace ExperienceGenerator.Client.Models
{
  using System.Collections.Generic;

  public class OptionTypes
  {
    public string Type { get; set; }

    public IEnumerable<SelectionOptionGroup> OptionGroups { get; set; }
  }
}