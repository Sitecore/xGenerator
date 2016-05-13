namespace ExperienceGenerator.Client.Models
{
  using System.Collections.Generic;

  public class ConfigurationOptions
  {
    public string Version { get; set; }
    public List<SelectionOption> Websites { get; set; }
    public List<SelectionOptionGroup> LocationGroups { get; set; }
    public List<SelectionOption> Campaigns { get; set; }
    public List<SelectionOptionGroup> ChannelGroups { get; set; }
    public List<SelectionOption> OrganicSearch { get; set; }
    public List<SelectionOption> PpcSearch { get; set; }
    public List<SelectionOptionGroup> OutcomeGroups { get; set; }
  }
}