namespace ExperienceGenerator.Client.Models
{
  using System.Collections.Generic;

  public class SelectionOptionGroup
  {
    public string Label { get; set; }

    public IEnumerable<SelectionOption> Options { get; set; }
  }

}